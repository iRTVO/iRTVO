using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Collections;
using System.Windows;

namespace iRTVO
{
    class remoteServer
    {
        public static Hashtable clientsList = new Hashtable();
        public static List<string> authorizedClients = new List<string>();

        public remoteServer(int port)
        {
            SharedData.serverOutBuffer.Clear();
            SharedData.executeBuffer.Clear();
            TcpListener serverSocket = new TcpListener(IPAddress.Any, port);
            TcpClient clientSocket = default(TcpClient);
            Thread servermessages = new Thread(selfcast);
            int counter = 0;

            try
            {
                serverSocket.Start();
            }
            catch
            {
                MessageBox.Show("Port already in use!");
                SharedData.serverThreadRun = false;
                return;
            }
            Console.WriteLine("Server started ....");
            counter = 0;

            servermessages.Start();

            while (SharedData.serverThreadRun)
            {
                if (serverSocket.Pending())
                {
                    counter += 1;
                    clientSocket = serverSocket.AcceptTcpClient();

                    byte[] bytesFrom = new byte[10025];
                    string dataFromClient = null;

                    NetworkStream networkStream = clientSocket.GetStream();
                    networkStream.Read(bytesFrom, 0, (int)clientSocket.ReceiveBufferSize);
                    dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);
                    dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
                    string[] cmd = dataFromClient.Split(';');

                    clientsList.Add(cmd[0], clientSocket);

                    if (cmd[1] == Properties.Settings.Default.remoteServerKey)
                    {
                        authorizedClients.Add(cmd[0]);
                        Console.WriteLine(dataFromClient + " connected AUTHORIZED!");
                    }
                    else
                        Console.WriteLine(dataFromClient + " connected ");

                    handleClient client = new handleClient();
                    client.startClient(clientSocket, cmd[0], clientsList);
                }
                Thread.Sleep(1000); // wait
            }

            serverSocket.Stop();
        }

        // server dumps own clicks to clients
        public void selfcast() {
            while (SharedData.serverThreadRun)
            {
                while (SharedData.serverOutBuffer.Count > 0)
                {
                    broadcast(SharedData.serverOutBuffer.Pop() + "$", "server");
                }
            }
        }

        public static void broadcast(string msg, string clNo)
        {
            foreach (DictionaryEntry Item in clientsList)
            {
                if ((string)Item.Key != clNo)
                {
                    TcpClient broadcastSocket;
                    broadcastSocket = (TcpClient)Item.Value;
                    NetworkStream broadcastStream = broadcastSocket.GetStream();

                    if (broadcastStream.CanWrite) {
                        Byte[] broadcastBytes = null;
                        broadcastBytes = Encoding.ASCII.GetBytes(msg);
                        broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);
                        broadcastStream.Flush();
                        Console.WriteLine("Broadcasting to " + Item.Key + " from " + clNo + " msg " + msg);
                    }
                }
            }
        }  //end broadcast function
    }//end Main class


    public class handleClient
    {
        TcpClient clientSocket;
        string clNo;
        Hashtable clientsList;

        public void startClient(TcpClient inClientSocket, string clientNo, Hashtable cList)
        {
            this.clientSocket = inClientSocket;
            this.clNo = clientNo;
            this.clientsList = cList;
            Thread ctThread = new Thread(doChat);
            ctThread.Start();
        }

        private void doChat()
        {
            int requestCount = 0;
            byte[] bytesFrom = new byte[10025];
            //string dataFromClient = null;
            string rCount = null;
            requestCount = 0;
            bool run = true;

            while (run)
            {
                if (!SharedData.serverThreadRun)
                    run = false;

                try
                {
                    if (clientSocket.Connected)
                    {
                        string dataFromClient = "";
                        requestCount = requestCount + 1;
                        NetworkStream networkStream = clientSocket.GetStream();
                        networkStream.ReadTimeout = 1000; // wait 1000ms max

                        if (networkStream.CanRead)
                        {
                            try
                            {
                                networkStream.Read(bytesFrom, 0, (int)clientSocket.ReceiveBufferSize);
                                dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);

                            }
                            catch (System.IO.IOException)
                            {
                                // skip timeout, preventing blocking
                            }

                            if (dataFromClient.IndexOf("$") >= 0 && remoteServer.authorizedClients.Contains(clNo))
                            {
                                dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
                                Console.WriteLine("From client " + clNo + ": " + dataFromClient);
                                rCount = Convert.ToString(requestCount);
                                SharedData.executeBuffer.Push(dataFromClient);
                                remoteServer.broadcast(dataFromClient + "$", clNo);
                            }
                        }
                    }
                    else
                    {
                        run = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    run = false;
                }
            }//end while
        }//end doChat
    } //end class handleClinet
}
