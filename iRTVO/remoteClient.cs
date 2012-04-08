using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Windows;

namespace iRTVO
{
    class remoteClient
    {

        System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        NetworkStream serverStream = default(NetworkStream);

        public remoteClient(string ip, int port) {
            while (true)
            {
                try
                {
                    clientSocket.Connect(IPAddress.Parse(ip), port);
                }
                catch
                {
                    Thread.Sleep(500);
                    continue;
                }

                // all is good
                break;
            }

            serverStream = clientSocket.GetStream();

            Thread ctThread = new Thread(getMessage);
            ctThread.Start();
        }

        private void getMessage()
        {
            // send id
            sendMessage(System.Guid.NewGuid().ToString() + ";" + Properties.Settings.Default.remoteClientKey);

            bool run = true;
            while (run)
            {
                serverStream = clientSocket.GetStream();
                int buffSize = 0;
                byte[] inStream = new byte[10025];
                buffSize = clientSocket.ReceiveBufferSize;
                try
                {
                    serverStream.Read(inStream, 0, buffSize);
                }
                catch {
                    run = false;
                }
                string returndata = System.Text.Encoding.ASCII.GetString(inStream);
                if (returndata.IndexOf("$") >= 0)
                {
                    returndata = returndata.Substring(0, returndata.IndexOf("$"));
                    if (returndata.Length > 0)
                        SharedData.executeBuffer.Push(returndata);
                }
            }
        }

        public void sendMessage(string msg) {
            byte[] outStream = System.Text.Encoding.ASCII.GetBytes(msg + "$");
            serverStream.Write(outStream, 0, outStream.Length);
            serverStream.Flush();
        }
    }
}
