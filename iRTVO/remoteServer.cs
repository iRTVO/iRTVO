using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Collections;
using System.Windows;

// for debug
using System.IO;


namespace iRTVO
{
    class remoteServer
    {
        public static Hashtable clientsList = new Hashtable();
        public static List<string> authorizedClients = new List<string>();

        public static TextWriter debug;

        public remoteServer(int port)
        {
            debug = new StreamWriter("server.log", true);

            SharedData.serverOutBuffer.Clear();
            SharedData.executeBuffer.Clear();

            debugLog("Listening to port " + port);

            TcpListener serverSocket = new TcpListener(IPAddress.Any, port);
            TcpClient clientSocket = default(TcpClient);
            Thread servermessages = new Thread(selfcast);
            int counter = 0;

            try
            {
                serverSocket.Start();
                debugLog("Socket created");
            }
            catch
            {
                MessageBox.Show("Port already in use!");
                SharedData.serverThreadRun = false;
                return;
            }
            counter = 0;

            servermessages.Start();

            while (SharedData.serverThreadRun)
            {
                if (serverSocket.Pending())
                {
                    counter += 1;
                    clientSocket = serverSocket.AcceptTcpClient();

                    byte[] bytesFrom = new byte[(int)clientSocket.ReceiveBufferSize];
                    string dataFromClient = null;

                    NetworkStream networkStream = clientSocket.GetStream();
                    networkStream.Read(bytesFrom, 0, (int)clientSocket.ReceiveBufferSize);
                    dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);


                    Int32 msglen = dataFromClient.IndexOf("$");
                    if (msglen > 0)
                    {
                        debugLog("Client connected");
                        dataFromClient = dataFromClient.Substring(0, msglen);

                        string[] cmd = dataFromClient.Split(';');

                        // clean up duplicates
                        if (clientsList.ContainsKey(cmd[0]))
                            clientsList.Remove(cmd[0]);

                        clientsList.Add(cmd[0], clientSocket);
                        debugLog("Added client " + cmd[0]);

                        if (cmd[1] == Properties.Settings.Default.remoteServerKey)
                        {
                            authorizedClients.Add(cmd[0]);
                            debugLog("Authorized client " + cmd[0]);
                        }

                        handleClient client = new handleClient();
                        client.startClient(clientSocket, cmd[0], clientsList);
                        debugLog("Client thread started for " + cmd[0]);
                    }
                    else
                    {
                        networkStream.Close();
                        debugLog("Invalid connection attempt");
                    }
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
                    debugLog("Broadcasting message");
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
                    if (broadcastSocket.Connected)
                    {
                        NetworkStream broadcastStream = broadcastSocket.GetStream();
                        Byte[] broadcastBytes = null;
                        broadcastBytes = Encoding.ASCII.GetBytes(msg);
                        broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);
                        broadcastStream.Flush();
                        debugLog("Broadcasting to " + Item.Key + " from " + clNo + " msg " + msg);
                    }
                }
            }
        }  //end broadcast function

        private static void debugLog(string msg)
        {
            remoteServer.debug.WriteLine(DateTime.Now.ToString("s") + " " + msg);
            remoteServer.debug.Flush();
        }
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
            debugLog("Client thread started");
        }

        private void doChat()
        {
            int requestCount = 0;
            
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
                        byte[] bytesFrom = new byte[(int)clientSocket.ReceiveBufferSize];
                        string dataFromClient = "";
                        requestCount = requestCount + 1;
                        if (clientSocket.Connected)
                        {
                            NetworkStream networkStream = clientSocket.GetStream();
                            networkStream.ReadTimeout = 1000; // wait 1000ms max

                            try
                            {
                                networkStream.Read(bytesFrom, 0, (int)clientSocket.ReceiveBufferSize);
                                dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);
                                debugLog("Received data from client");

                            }
                            catch (System.IO.IOException)
                            {
                                // skip timeout, preventing blocking
                            }

                            if (dataFromClient.IndexOf("$") >= 0 && remoteServer.authorizedClients.Contains(clNo))
                            {
                                dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
                                rCount = Convert.ToString(requestCount);

                                debugLog("Executing command '" + dataFromClient + "'");
                                SharedData.executeBuffer.Push(dataFromClient);

                                debugLog("Broadcasting recv command '" + dataFromClient + "'");
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

        private static void debugLog(string msg)
        {
            remoteServer.debug.WriteLine(DateTime.Now.ToString("s") + " " + msg);
            remoteServer.debug.Flush();
        }
    } //end class handleClinet
}
