using iRTVO.Interfaces;
using iRTVO.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class SharedData
    {
        public static Settings settings;

        // Mutexes
        public static Mutex mutex = new Mutex();
        public static object SharedDataLock = new object();

        // API state
        public static Boolean runApi = true;
        public static Boolean runOverlay = false;
        public static Boolean apiConnected = false;

        // Overlay performance timers
        public static int cacheHit = 0;
        public static int cacheMiss = 0;
        public static int cacheFrameCount = 0;
        public static int overlayUpdateTime = 0;

        // Theme
        public static Theme theme;
        public static Boolean refreshButtons = false;
        public static Boolean refreshTheme = false;
        public static int replayRewind = 0;
        public static Boolean inReplay = false;
        private static int overlaySession = 0;

        public static int OverlaySession
        {
            get { return overlaySession; }
            set
            {
                if (value != overlaySession)
                {
                    overlaySession = value;
                    iRTVOConnection.BroadcastMessage("CHGSESSION", value);
                }
            }
        }


        public static string overlayClass = null;

        public static Dictionary<Theme.sessionType, int> sessionTypes = new Dictionary<Theme.sessionType, int>()
        {
            {Theme.sessionType.none, 0},
            {Theme.sessionType.practice, 0},
            {Theme.sessionType.qualify, 0},
            {Theme.sessionType.race, 0}
        };

        public static Boolean[] lastPage;
        public static String[][][] themeDriverCache = new string[64][][];
        public static String[] themeSessionStateCache = new string[0];
        public static Double themeCacheSessionTime = 0;
        public static Stack triggers = new Stack();
        public static Double currentSessionTime = 0;

        // allow retirement
        public static Boolean allowRetire = false;

        // csv
        public static Dictionary<int, string[]> externalData = new Dictionary<int, string[]>();
        public static Dictionary<int, int> externalPoints = new Dictionary<int, int>();
        public static Dictionary<int, int> externalCurrentPoints = new Dictionary<int, int>();

        // web timing
        public static WebTiming.webTiming web;
        public static Int64 webBytes = 0;
        public static String webError = "";

        // Data
        public static List<DriverInfo> Drivers = new List<DriverInfo>();
        public static Sessions Sessions = new Sessions();
        public static TrackInfo Track = new TrackInfo();
        public static CameraInfo Camera = new CameraInfo();
        public static List<SessionEvent> Events = new List<SessionEvent>();
        public static Bookmarks Bookmarks = new Bookmarks();
        public static List<Single> Sectors = new List<Single>();
        public static List<Single> SelectedSectors = new List<Single>();
        public static Int32[] Classes = new Int32[3] { -1, -1, -1 };
        public static Dictionary<string, int> ClassOrder = new Dictionary<string, int>();
        public static TimeDelta timedelta = new TimeDelta(1000, 10, 64);

        public static int currentRadioTransmitcarIdx = -1;
        public static int currentFollowedDriver = -1;
        public static int currentCam = -1;
        public static int selectedPlaySpeed = 1;

        // Update stuff
        public static Boolean updateControls = false;
        public static Boolean showSimUi = true;
        public static Boolean[] tickerReady;

        // TCP
        public static Boolean remoteClientFollow = true;
        public static Boolean remoteClientSkipRewind = false;

        // Scripting
        public static Scripting scripting;

        public static Boolean readCache(Int32 sessionId)
        {
            string cachefilename = Directory.GetCurrentDirectory() + "\\cache\\" + sessionId + "-sessions.xml";
            if (File.Exists(cachefilename))
            {
                FileStream fs = new FileStream(cachefilename, FileMode.Open);
                TextReader reader = new StreamReader(fs);
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(SharedData.Sessions.GetType());
                SharedData.Sessions = (Sessions)x.Deserialize(reader);
                fs.Close();
            }
            else
                return false;

            cachefilename = Directory.GetCurrentDirectory() + "\\cache\\" + sessionId + "-drivers.xml";
            if (File.Exists(cachefilename))
            {
                FileStream fs = new FileStream(cachefilename, FileMode.Open);
                TextReader reader = new StreamReader(fs);
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(SharedData.Drivers.GetType());
                SharedData.Drivers = (List<DriverInfo>)x.Deserialize(reader);
                fs.Close();
            }
            else
                return false;

            return true;
        }

        public static void writeCache(Int32 sessionId)
        {
            DirectoryInfo di = Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\cache\\");
            TextWriter tw = new StreamWriter(Directory.GetCurrentDirectory() + "\\cache\\" + sessionId + "-sessions.xml");
            System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(SharedData.Sessions.GetType());
            SharedData.Sessions = new Sessions();
            x.Serialize(tw, SharedData.Sessions);
            tw.Close();

            tw = new StreamWriter(Directory.GetCurrentDirectory() + "\\cache\\" + sessionId + "-drivers.xml");
            x = new System.Xml.Serialization.XmlSerializer(SharedData.Drivers.GetType());
            SharedData.Drivers = new List<DriverInfo>();
            x.Serialize(tw, SharedData.Drivers);
            tw.Close();
        }

        public static event PropertyChangedEventHandler PropertyChanged;
        public static void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(null, new PropertyChangedEventArgs(name));
        }
    }


    public class SharedData_Public : ISharedData
    {

        public ICameraInfo Camera
        {
            get
            {
                return SharedData.Camera;
            }
        }

        public IList<IDriverInfo> Drivers
        {
            get { return SharedData.Drivers as IList<IDriverInfo>; }
        }

        public IList<ISessionEvent> Events
        {
            get { return SharedData.Events as IList<ISessionEvent>; }
        }

        public ISessions Sessions
        {
            get { return SharedData.Sessions; }
        }

        public ITrackInfo Track
        {
            get { return SharedData.Track; }
        }
    }


}
