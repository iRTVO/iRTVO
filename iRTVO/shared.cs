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
using System.Collections.Generic;
using System.Linq;
using System.Text;

// additional
using System.Threading;

namespace iRTVO
{
    class SharedData
    {
        public struct DriverInfo
        {
            public string name;
            public string initials;
            public string shortname;

            public string club;
            public string car;
            public int carclass;
            public string license;

            public int userId;
            public int carId;
            public int numberPlate;

            public Boolean onTrack;
            public DateTime offTrackSince;

            public DriverInfo(int none)
            {
                this.name = "";
                this.initials = "";
                this.shortname = "";

                this.club = "";
                this.car = "";
                this.carclass = 0;
                this.license = "R0.00";

                this.userId = 0;
                this.carId = 0;
                this.numberPlate = 0;

                this.onTrack = true;
                this.offTrackSince = DateTime.Now;
            }

        }

        public struct LapInfo
        {
            public int id;
            public float diff;
            public int lapDiff;
            public float completedLaps;
            public float fastLap;
            public int lapsLed;
            public float previouslap;
            public DateTime lastNewLap;
            public int lastNewLapNr;


            public LapInfo(int none)
            {
                this.id = 0;
                this.diff = 0.0f;
                this.lapDiff = 0;
                this.completedLaps = 0.0f;
                this.fastLap = 0.0f;
                this.lapsLed = 0;
                this.previouslap = 0.0f;
                this.lastNewLap = DateTime.Now;
                this.lastNewLapNr = -1;
            }
        }

        public struct SessionInfo
        {
            public int sessionId;
            public int subSessionId;

            public int laps;
            public int lapsRemaining;

            public float time;
            public float timeRemaining;

            public iRacingTelem.eSessionType type;
            public iRacingTelem.eSessionState state;
            public iRacingTelem.eSessionFlag flag;

            public int driverFollowed;

            public int cautions;
            public int cautionLaps;
            public int leadChanges;

            public Boolean official;

            public SessionInfo(int none)
            {
                this.sessionId = 0;
                this.subSessionId = 0;

                this.laps = 0;
                this.lapsRemaining = 0;

                this.time = 0.0f;
                this.timeRemaining = 0.0f;

                this.type = iRacingTelem.eSessionType.kSessionTypeInvalid;
                this.state = iRacingTelem.eSessionState.kSessionStateInvalid;
                this.flag = iRacingTelem.eSessionFlag.kFlagGreen;

                this.driverFollowed = 0;

                this.cautions = 0;
                this.cautionLaps = 0;
                this.leadChanges = 0;

                this.official = false;
            }
        }

        public struct TrackInfo
        {
            public string name;
            public int id;
            public float length;
        }

        public enum ConnectionState
        {
            initializing = 0,
            connecting,
            active,
        }

        // DriverInfo
        public static Mutex driversMutex;
        public static DriverInfo[] drivers = new DriverInfo[iRacingTelem.MAX_CARS];

        // LapInfo
        public static Mutex standingMutex;
        public static LapInfo[][] standing = new LapInfo[iRacingTelem.MAX_SESSIONS][];

        // SessionInfo
        public static Mutex sessionsMutex;
        public static SessionInfo[] sessions = new SessionInfo[iRacingTelem.MAX_SESSIONS];
        public static int currentSession;

        // TrackInfo
        public static Mutex trackMutex;
        public static TrackInfo track = new TrackInfo();

        // API state
        public static Boolean runApi = true;
        public static Boolean runOverlay = false;
        public static ConnectionState apiState;

        // Start light timer
        public static DateTime startlights;

        // Overlay performance timers
        public static Stack<float> overlayFPSstack = new Stack<float>();
        public static Stack<float> overlayEffectiveFPSstack = new Stack<float>();

        // Theme
        public static Theme theme;
        public static Boolean refreshButtons = false;
        public static Boolean refreshTheme = false;
        
        public static int overlaySession = 0;
        public static Dictionary<Theme.sessionType, int> sessionTypes = new Dictionary<Theme.sessionType, int>()
        {
            {Theme.sessionType.none, 0},
            {Theme.sessionType.practice, 0},
            {Theme.sessionType.qualify, 0},
            {Theme.sessionType.race, 0}
        };

        public static Boolean[] lastPage;
        
        // replay
        public static Boolean replayInProgress = false;
        public static ManualResetEvent replayReady = new ManualResetEvent(false);

        // allow retirement
        public static Boolean allowRetire = false;

        // csv
        public static Dictionary<int, string[]> externalData = new Dictionary<int, string[]>();

        // web timing
        public static webTiming web;
        public static Boolean[] webUpdateWait = new Boolean[Enum.GetValues(typeof(webTiming.postTypes)).Length];
        public static Int64 webBytes = 0;
        public static String webError;
    }
}
