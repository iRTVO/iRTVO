using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace iRTVO
{
    class remoteClient
    {

        TcpClient client;
        IPEndPoint serverEndPoint;
        NetworkStream clientStream;

        public remoteClient(string ip, int port) {
            this.client = new TcpClient();
            this.serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

            while (true)
            {
                try
                {
                    this.client.Connect(serverEndPoint);
                }
                catch
                {
                    Thread.Sleep(500);
                    continue;
                }

                // all is good
                break;
            }

            this.clientStream = client.GetStream();
        }

        public void SendMessage(string message) {
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] buffer = encoder.GetBytes(message);

            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
        }
    }
}
