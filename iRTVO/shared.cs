/*
 * shared.cs
 * 
 * SharedData class:
 * 
 * Holds the data structures which are shared between API and overlay.
 * 
 * API uses mutexes while writing to the DriverInfo, LapInfo, SessionInfo and TrackInfo structures.
 * 
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// additional
using System.Threading;
using iRSDKSharp;
using System.ComponentModel;
using System.IO;

namespace iRTVO
{
    class SharedData
    {
        public static Settings settings = new Settings(Directory.GetCurrentDirectory() + "\\options.ini");

        // Mutexes
        public static Mutex mutex = new Mutex();

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
        public static int overlaySession = 0;
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
        public static Dictionary<int, int> externalPoints = new Dictionary<int,int>();
        public static Dictionary<int, int> externalCurrentPoints = new Dictionary<int, int>();

        // web timing
        public static webTiming web;
        public static Int64 webBytes = 0;
        public static String webError = "";

        // Data
        public static List<DriverInfo> Drivers = new List<DriverInfo>();
        public static Sessions Sessions = new Sessions();
        public static TrackInfo Track = new TrackInfo();
        public static CameraInfo Camera = new CameraInfo();
        public static Events Events = new Events();
        public static Bookmarks Bookmarks = new Bookmarks();
        public static List<Single> Sectors = new List<Single>();
        public static List<Single> SelectedSectors = new List<Single>();
        public static Int32[] Classes = new Int32[3] {-1, -1, -1};
        public static Dictionary<string, int> ClassOrder = new Dictionary<string, int>();
        public static TimeDelta timedelta = new TimeDelta(1000, 10, 64);

        // Update stuff
        public static Boolean updateControls = false;
        public static Boolean showSimUi = true;
        public static Boolean[] tickerReady;

        // TCP
        //public static Stack<String> executeBuffer = new Stack<string>();
        public static Dictionary<string, string> executeBuffer = new Dictionary<string, string>();
        public static Stack<String> serverOutBuffer = new Stack<string>();
        public static Thread serverThread = null;
        public static remoteClient remoteClient = null;
        public static Boolean serverThreadRun = false;
        public static Boolean remoteClientFollow = true;
        public static Boolean remoteClientSkipRewind = false;

        // Scripting
        public static Scripting scripting;

        public static Boolean readCache(Int32 sessionId) {
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
            SharedData.Sessions = new iRTVO.Sessions();
            x.Serialize(tw, SharedData.Sessions);
            tw.Close();

            tw = new StreamWriter(Directory.GetCurrentDirectory() + "\\cache\\" + sessionId + "-drivers.xml");
            x = new System.Xml.Serialization.XmlSerializer(SharedData.Drivers.GetType());
            SharedData.Drivers = new List<DriverInfo>();
            x.Serialize(tw, SharedData.Drivers);
            tw.Close();
        }
    }
}
