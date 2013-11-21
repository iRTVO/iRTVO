using DPSBase;
using NetworkCommsDotNet;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace iRTVO.Networking
{
    public delegate void OnProcessMessage(iRTVORemoteEvent m);
    public delegate void OnClientConnectionEstablished();
    public delegate void OnNewClient(string newClientID);

    public class iRTVOConnection
    {
        private static event OnProcessMessage _ProcessMessage;
        public static event OnProcessMessage ProcessMessage
        {
            add
            {
                _ProcessMessage -= value;
                _ProcessMessage += value;
            }
            remove
            {
                _ProcessMessage -= value;
            }
        }

        private static event OnNewClient _NewClient;
        public static event OnNewClient NewClient
        {
            add
            {
                _NewClient -= value;
                _NewClient += value;
            }
            remove
            {
                _NewClient -= value;
            }
        }

        private static event OnClientConnectionEstablished _ClientConnectionEstablished;
        public static event OnClientConnectionEstablished ClientConnectionEstablished
        {
            add
            {
                _ClientConnectionEstablished -= value;
                _ClientConnectionEstablished += value;
            }
            remove
            {
                _ClientConnectionEstablished -= value;
            }
        }

        private static string _Password;

        public static bool isServer { get; private set; }

        public static bool isConnected
        {
            get
            {
                if (isServer) return true;
                if ((serverConnection == null) || (serverConnection.ConnectionInfo.ConnectionState != ConnectionState.Established))
                    return false;
                return true;
            }
        }

        public static bool isAvailable
        {
            get 
            {
                if (isServer)
                    return false;
                if ( !isServer && (serverConnection==null) )
                    return true;
                return false;
            }

        }

        private static Dictionary<ShortGuid,bool> isAuthenticated = new Dictionary<ShortGuid,bool>();
        private static Connection serverConnection = null;

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        private static void HandleIncomingMessage(PacketHeader header, Connection connection, iRTVOMessage incomingMessage)
        {
            logger.Log(NLog.LogLevel.Debug,"HandleIncomingMessage: {0}", incomingMessage.ToString());
            
            if (isServer && (incomingMessage.Command == "AUTHENTICATE"))
            {
                if ((incomingMessage.Arguments == null) || (incomingMessage.Arguments.Count() != 1))
                {
                    logger.Error("HandleIncomingMessage: Wrong arguments to Authenticate from {0}", connection.ConnectionInfo.NetworkIdentifier);
                    connection.CloseConnection(false,-100);
                    return;
                }
                if (String.Compare(_Password, Convert.ToString(incomingMessage.Arguments[0])) != 0)
                {
                    logger.Error("HandleIncomingMessage: Worng Password from {0}", connection.ConnectionInfo.NetworkIdentifier);
                    connection.CloseConnection(false,-200);
                }
                logger.Info("Client {0} authenticated.", connection.ConnectionInfo.NetworkIdentifier);
                isAuthenticated[connection.ConnectionInfo.NetworkIdentifier] = true;
                connection.SendObject("iRTVOMessage", new iRTVOMessage(NetworkComms.NetworkIdentifier, "AUTHENTICATED"));
                if (_NewClient != null)
                    _NewClient(connection.ConnectionInfo.NetworkIdentifier);
                return;
            }

            if (!isServer && (incomingMessage.Command == "AUTHENTICATED"))
            {
                if (_ClientConnectionEstablished != null)
                    _ClientConnectionEstablished();
                return;
            }

            if (isServer && (!isAuthenticated.ContainsKey(connection.ConnectionInfo.NetworkIdentifier) ||  !isAuthenticated[connection.ConnectionInfo.NetworkIdentifier]))
            {
                logger.Warn("HandleIncomingMessage: Command from unauthorized client {0}",connection.ConnectionInfo.NetworkIdentifier);
                connection.CloseConnection(false,-300);
                return;
            }

            iRTVORemoteEvent e = new iRTVORemoteEvent(incomingMessage);
            if (_ProcessMessage != null)
            {
                using ( TimeCall tc = new TimeCall("ProcessMessage") )
                    _ProcessMessage(e);
            }
            // Handler signals to abort this connection!
            if (e.Cancel)
            {
                logger.Error("HandleIncomingMessage: ProcessMessage signaled to close client {0}", connection.ConnectionInfo.NetworkIdentifier);
                connection.CloseConnection(true, -400);
            }
            else
            {
               
                if (isServer && e.Forward)
                    ForwardMessage(incomingMessage);
               
            }
        }

        public static string MyNetworkID { get { return NetworkComms.NetworkIdentifier.ToString(); } }

        public static void ProcessInternalMessage( iRTVOMessage incomingMessage )
        {
            if (isServer || (!isConnected))
            {
                iRTVORemoteEvent e = new iRTVORemoteEvent(incomingMessage);
                if (_ProcessMessage != null)
                {
                    using (TimeCall tc = new TimeCall("ProcessInternalMessage"))
                    {
                        _ProcessMessage(e);
                        if (isServer && e.Forward)
                            ForwardMessage(incomingMessage);
                    }
                }
            }
            else
            {
                BroadcastMessage(incomingMessage);

            }
        }

        private static void HandleAuthenticateRequest(PacketHeader header, Connection connection, object incomingMessage)
        {
            logger.Info("Authentication Request Received from {0} ({1})", connection.ConnectionInfo.NetworkIdentifier, connection);
            
            connection.SendObject("iRTVOMessage", new iRTVOMessage(NetworkComms.NetworkIdentifier,"AUTHENTICATE",_Password));
        }


        private static void HandleNewConnection(Connection connection)
        {
            if (isServer) // if we are the server, start the handshake
            {
                logger.Info("Connection from {0} ({1}).", connection.ConnectionInfo.NetworkIdentifier,connection.ConnectionInfo);
                connection.SendObject("iRTVOAuthenticate"); // Signal client that it must authenticate to us;                
            }
            else
            {
                logger.Info("Connection to Server established");
                serverConnection = connection;
            }
        }

        private static void HandleConnectionClosed(Connection connection)
        {
            if (!isServer)
            {
                serverConnection = null;
                logger.Info("Connection to server lost");
            }
            else
            {
                logger.Info("Client {0} disconnected.", connection);
            }
        }

        public static bool StartServer(int Port, string Password)
        {
            try
            {                
                _Password = Password;
                logger.Info("Starting Server");
                
                NetworkComms.AppendGlobalConnectionEstablishHandler(HandleNewConnection, false);
                NetworkComms.AppendGlobalConnectionCloseHandler(HandleConnectionClosed);
                NetworkComms.AppendGlobalIncomingPacketHandler<iRTVOMessage>("iRTVOMessage", HandleIncomingMessage);
                                   
                NetworkComms.DefaultListenPort = Port;
                TCPConnection.StartListening(false);
                isAuthenticated[NetworkComms.NetworkIdentifier] = true; // Server is always authenticated!
            }
            catch (CommsSetupShutdownException ex)
            {
                logger.Error("Failed to start Server: {0}", ex.Message);
                
                return false;
            }
            isServer = true;
            return true;
        }

        public static bool StartClient(string Hostname, int Port, string Password)
        {
            try
            {
                logger.Info("Trying to connect to Server");
                _Password = Password;
                isServer = false;

                IPAddress ipAddress = null;
                try
                {
                    ipAddress = IPAddress.Parse(Hostname);
                }
                catch (FormatException)
                {
                    // Improperly formed IP address.
                    // Try resolving as a domain.
                    ipAddress = Dns.GetHostEntry(Hostname).AddressList[0];
                }

                ConnectionInfo serverInfo = new ConnectionInfo(ipAddress.ToString(), Port);
                NetworkComms.AppendGlobalConnectionEstablishHandler(HandleNewConnection, false);
                NetworkComms.AppendGlobalIncomingPacketHandler<iRTVOMessage>("iRTVOMessage", HandleIncomingMessage);
                NetworkComms.AppendGlobalIncomingPacketHandler<object>("iRTVOAuthenticate", HandleAuthenticateRequest);
                NetworkComms.AppendGlobalConnectionCloseHandler(HandleConnectionClosed);
                TCPConnection.GetConnection(serverInfo, true);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to connect to Server: {0}", ex.Message);
                return false;
            }
            return true;
        }

        public static void Close()
        {
            try
            {
                if (isServer)
                {
                    NetworkComms.Shutdown();
                    isServer = false;
                    return;
                }
                if (serverConnection != null)
                {
                    serverConnection.CloseConnection(false, -700);
                    serverConnection = null;
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error in Close: {0}", ex.ToString());
            }
        }

        public static void BroadcastMessage(iRTVOMessage m)
        {
            if (isServer) // Server Broadcasts to all clients
            {

                var allRelayConnections = NetworkComms.GetExistingConnection();// (from current in NetworkComms.GetExistingConnection() where current != connection select current).ToArray();


                logger.Debug("Broadcasting Command {0} to {1} Clients", m.Command, allRelayConnections.Count);
                //We will now send the message to every other connection
                foreach (var relayConnection in allRelayConnections)
                {
                    //We ensure we perform the send within a try catch
                    //To ensure a single failed send will not prevent the
                    //relay to all working connections.
                    try { relayConnection.SendObject("iRTVOMessage", m); }
                    catch (CommsException ex)
                    {
                        logger.Warn("Broadcast to {0} failed: {1}", relayConnection, ex.ToString());
                    }
                }

                return;
            }
            else // Client only sends to server
            {
                if (serverConnection != null)
                {
                    logger.Debug("Sending Command '{0}' to Server", m.Command);
                    try { serverConnection.SendObject("iRTVOMessage", m); }
                    catch (CommsException ex)
                    {
                        logger.Error("Sending of Command '{0}' failed: {1}", m.Command, ex.ToString());
                    }
                }
            }
        }

        public static void BroadcastMessage( string Command , params object[] Arguments )
        {
            string[] args = new string[ Arguments.Length ];
            for(int i=0;i < Arguments.Length; i++)
                args[i] = Convert.ToString(Arguments[i]);

            iRTVOMessage m = new iRTVOMessage(NetworkComms.NetworkIdentifier, Command, args);

            BroadcastMessage(m);

        }

        private static void ForwardMessage(iRTVOMessage incomingMessage)
        {
            var allRelayConnections = NetworkComms.GetExistingConnection();// (from current in NetworkComms.GetExistingConnection() where current != connection select current).ToArray();
            incomingMessage.Source = NetworkComms.NetworkIdentifier;

            logger.Info("Forwarding Command {0} to {1} Clients", incomingMessage.Command, allRelayConnections.Count);
            //We will now send the message to every other connection
            foreach (var relayConnection in allRelayConnections)
            {
                if (relayConnection.ConnectionInfo.NetworkIdentifier == incomingMessage.Source)
                {
                    logger.Debug("Not sending redundant message to {0}", incomingMessage.Source);
                    continue;
                }
                //We ensure we perform the send within a try catch
                //To ensure a single failed send will not prevent the
                //relay to all working connections.
                try { relayConnection.SendObject("iRTVOMessage", incomingMessage); }
                catch (CommsException ex)
                {
                    logger.Warn("Broadcast to {0} failed: {1}", relayConnection, ex.ToString());
                }
            }
        }

        public static void SendMessage(string targetClient, string Command , params object[] Arguments )
        {
            if (!isServer)
                throw new UnauthorizedAccessException("Only the server can call this function!");

            string[] args = new string[ Arguments.Length ];
            for(int i=0;i < Arguments.Length; i++)
                args[i] = Convert.ToString(Arguments[i]);

            iRTVOMessage m = new iRTVOMessage(NetworkComms.NetworkIdentifier, Command, args);
        
            var allRelayConnections = NetworkComms.GetExistingConnection();// (from current in NetworkComms.GetExistingConnection() where current != connection select current).ToArray();


            logger.Info("Sending Command {0} to {1} ", Command, targetClient);
            //We will now send the message to every other connection
            foreach (var relayConnection in allRelayConnections)
            {
                if (relayConnection.ConnectionInfo.NetworkIdentifier != targetClient)
                {                    
                    continue;
                }
                //We ensure we perform the send within a try catch
                //To ensure a single failed send will not prevent the
                //relay to all working connections.
                try { relayConnection.SendObject("iRTVOMessage", m); }
                catch (CommsException ex)
                {
                    logger.Warn("Broadcast to {0} failed: {1}", relayConnection, ex.ToString());
                }
            }
        }

        public static void Shutdown()
        {
            Close();
            NetworkComms.Shutdown();
        }
    }
}
