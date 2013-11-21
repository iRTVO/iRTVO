using iRTVO.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iRTVORaceControl
{
    class Program
    {
        static Timer joystickPollTimer = null;
        static Joystick localStick = null;
        
        static Dictionary<string, int> iRacingCameras = new Dictionary<string,int>();
        static Dictionary<string, int> iRacingDrivers = new Dictionary<string,int>();
        static Dictionary<string, string> iRacingButtons = new Dictionary<string,string>();

        static void Main(string[] args)
        {           

            Dictionary<Guid, string> foundSticks = Joystick.GetAllJoysticks();
            foreach (KeyValuePair<Guid, string> pair in foundSticks)
            {
                if (pair.Value == "SymProjects JC32")
                {
                    localStick = new Joystick(pair.Key);
                    Console.WriteLine(String.Format("JoyStick: {0} {1}", pair.Key.ToString("b"), pair.Value));
                    localStick.ButtonDown += localStick_ButtonDown;
                    Console.WriteLine("Starting Joystick Timer.");
                    joystickPollTimer = new Timer(JoystickPollTimerCallback, 1, 1000, 10);
                }
            }

            iRTVOConnection.ClientConnectionEstablished += iRTVOConnection_ClientConnectionEstablished;
            iRTVOConnection.ProcessMessage += iRTVOConnection_ProcessMessage;
            if (!iRTVOConnection.StartClient("192.168.178.27", 17198, "VR"))
            {
                Console.WriteLine("Could not connect to server");
                return;
            }

            Console.WriteLine("Press enter to close ...");

            Console.ReadLine();

            iRTVOConnection.Close();
            iRTVOConnection.Shutdown();

            if (localStick != null)
            {
                joystickPollTimer.Dispose();
                localStick = null;
            }
        }

        static void iRTVOConnection_ProcessMessage(iRTVORemoteEvent e)
        {
            Console.WriteLine("Received Command: " + e.Message.Command);
            switch (e.Message.Command.ToUpperInvariant())
            {               
                case "ADDCAM":
                    {
                        iRacingCameras[e.Message.Arguments[1].ToLowerInvariant()] = Convert.ToInt32(e.Message.Arguments[0]);
                        break;
                    }
                case "ADDDRIVER":
                    {
                        iRacingDrivers[e.Message.Arguments[1].ToLowerInvariant()] = Convert.ToInt32(e.Message.Arguments[0]);
                        break;
                    }
                case "ADDBUTTON":
                    {                        
                        iRacingButtons[e.Message.Arguments[1].ToLowerInvariant()] = e.Message.Arguments[0];
                        break;
                    }

                default:
                    break;

            }
        }

        static void iRTVOConnection_ClientConnectionEstablished()
        {
            iRTVOConnection.BroadcastMessage("SENDCAMS");
            iRTVOConnection.BroadcastMessage("SENDDRIVERS");
            iRTVOConnection.BroadcastMessage("SENDBUTTONS");
        }

        

        private static void JoystickPollTimerCallback(Object state)
        {
            localStick.Tick(null, null);
        }


        static void localStick_ButtonDown(int ButtonNumber, Joystick joystick)
        {
            int curCam = 0;
            int CurDriver = 0;

            Console.WriteLine("ButtonDown " + ButtonNumber);

            switch (ButtonNumber)
            {
                case 7:
                    iRTVOConnection.BroadcastMessage("BUTTON" , FindButton("Standings"));
                    break;
                case 14:
                    iRTVOConnection.BroadcastMessage("BUTTON" , FindButton("Ticker"));
                    break;
                case 13:
                    iRTVOConnection.BroadcastMessage("BUTTON" , FindButton("Fastest Lap"));
                    break;
                case 27:
                    { // Exiting
                        CurDriver = -1;
                        break;
                    }
                case 28:
                    { // ME
                        CurDriver = FindDriver("Ulrich Strauss3");
                        break;
                    }
                case 31:
                    { // LEADEER
                        CurDriver = -2;
                        break;
                    }
                case 26:
                    { // TV1
                        curCam = FindCamera("TV1");
                        break;
                    }
                case 29:
                    { // COCK
                        curCam = FindCamera("Cockpit");
                        break;
                    }
                case 30:
                    { // GYRO
                        curCam = FindCamera("RandomTV");
                        break;
                    }
                default: break;
            }
            if ( curCam != 0)
                iRTVOConnection.BroadcastMessage("CAMERA" , curCam);
            if ( CurDriver != 0)
                iRTVOConnection.BroadcastMessage("DRIVER" , CurDriver);
        }

        private static int FindCamera(string p)
        {
           
            if ( iRacingCameras.ContainsKey(p.ToLowerInvariant()))
                return iRacingCameras[ p.ToLowerInvariant() ];

            return -1;
        }

        

        private static int FindDriver(string p)
        {
            if (iRacingDrivers.ContainsKey(p.ToLowerInvariant()))
                return iRacingDrivers[p.ToLowerInvariant()];

            return -1;
        }
        private static string FindButton(string p)
        {
            if (iRacingButtons.ContainsKey(p.ToLowerInvariant()))
                return iRacingButtons[p.ToLowerInvariant()];

            return String.Empty;
        }

    }
}
