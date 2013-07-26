using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Windows;

// for debug
using System.IO;

namespace iRTVO
{
    class remoteClient
    {

        System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        NetworkStream serverStream = default(NetworkStream);

        private static TextWriter debug;

        public remoteClient(string ip, int port) {

            debug = new StreamWriter("client.log", true);

            SharedData.serverOutBuffer.Clear();
            SharedData.executeBuffer.Clear();

            IPAddress ipAddress = null;
            try
            {
                ipAddress = IPAddress.Parse(ip);
            }
            catch (FormatException fe)
            {
                // Improperly formed IP address.
                // Try resolving as a domain.
                ipAddress = Dns.GetHostEntry(ip).AddressList[0];
            }

            Stopwatch timer = new Stopwatch();
            timer.Start();
            while (true)
            {
                try
                {
                    debugLog("Connecting to " + ip + ":" + port);
                    clientSocket.Connect(ipAddress, port);
                }
                catch (Exception e)
                {
                    // timeout if this goes on too long to prevent freezing the system in infinite loop
                    if (timer.ElapsedMilliseconds > 10000)
                    {
                        timer.Stop();
                        debugLog(e.StackTrace);                        
                        throw e;
                    }
                    Thread.Sleep(500);
                    continue;
                }

                // all is good
                break;
            }
            timer.Stop();

            if (clientSocket.Connected)
            {
                serverStream = clientSocket.GetStream();
                debugLog("Connected succesfully");

                debugLog("Starting client thread");
                Thread ctThread = new Thread(getMessage);
                ctThread.Start();
            }
        }

        private void getMessage()
        {
            // send id
            debugLog("Sending initialization");

            if(Properties.Settings.Default.remoteClientName == "null") {
                Properties.Settings.Default.remoteClientName = System.Guid.NewGuid().ToString();
                Properties.Settings.Default.Save();
            }

            sendMessage(Properties.Settings.Default.remoteClientName + ";" + SharedData.settings.RemoteControlClientPassword);

            bool run = true;
            while (run)
            {
                if(clientSocket.Connected) 
                {
                    serverStream = clientSocket.GetStream();
                    int buffSize = 0;
                    byte[] inStream = new byte[10025];
                    buffSize = clientSocket.ReceiveBufferSize;
                    try
                    {
                        debugLog("Reading stream");
                        serverStream.Read(inStream, 0, buffSize);
                    }
                    catch
                    {
                        debugLog("Reading failed");
                        run = false;
                    }

                    string returndata = System.Text.Encoding.ASCII.GetString(inStream);
                    if (returndata.IndexOf("$") >= 0)
                    {
                        returndata = returndata.Substring(0, returndata.IndexOf("$"));
                        debugLog("Got data: '" + returndata + "'");
                        if (returndata.Length > 0)
                            SharedData.executeBuffer.Push(returndata);
                    }
                }
            }
        }

        public void sendMessage(string msg) {
                byte[] outStream = System.Text.Encoding.ASCII.GetBytes(msg + "$");
                debugLog("Sending data: '" + msg + "$'");
                serverStream.Write(outStream, 0, outStream.Length);
                serverStream.Flush();
        }

        private void debugLog(string msg) {
            debug.WriteLine(DateTime.Now.ToString("s") + " " +  msg);
            debug.Flush();
        }
    }
}
