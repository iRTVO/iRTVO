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
        public struct Driver
        {
            public string name;
            public string initials;
            public string shortname;

            public string club;
            public string car;
            public int carclass;
            public string license;

            public float fastestlap;
            public float previouslap;
            public int completedlaps;
            public DateTime lastNewLap;
            public int lastNewLapNr;

            public int userId;
            public int carId;

            public Boolean onTrack;
            public DateTime offTrackSince;

        }

        public struct LapInfo
        {
            public int id;
            public float diff;
            public int lapDiff;
            public float completedLaps;
            public float fastLap;
        }

        public struct SessionInfo
        {
            public int laps;
            public int lapsRemaining;

            public float time;
            public float timeRemaining;

            public iRacingTelem.eSessionType type;
            public iRacingTelem.eSessionState state;
            public iRacingTelem.eSessionFlag flag;

            public int driverFollowed;
        }

        public enum overlayObjects
        {
            driver,
            sidepanel,
            results,
            replay,
            sessionstate,
            flag,
            startlights,
            ticker,
            laptime
        }

        public enum sidepanelTypes
        {
            leader,
            followed,
            fastlap
        }

        public enum ConnectionState
        {
            initializing = 0,
            connecting,
            active,
        }

        // Driver
        public static Mutex driversMutex;
        public static Driver[] drivers = new Driver[iRacingTelem.MAX_CARS];
        //public static Boolean driversUpdated = false;

        // LapInfo
        public static Mutex standingMutex;
        public static LapInfo[][] standing = new LapInfo[iRacingTelem.MAX_SESSIONS][];
        //public static Boolean standingsUpdated = false;

        // SessionInfo
        public static Mutex sessionsMutex;
        public static SessionInfo[] sessions = new SessionInfo[iRacingTelem.MAX_SESSIONS];
        public static int currentSession;
        //public static Boolean sessionsUpdated = false;

        //public static Boolean refreshOverlay = false;

        // States
        public static Boolean[] visible = new Boolean[Enum.GetValues(typeof(overlayObjects)).Length];
        public static sidepanelTypes sidepanelType = sidepanelTypes.leader;

        public static int resultPage = -1;
        public static Boolean resultLastPage = false;
        public static int resultSession = -1;

        public static Boolean runApi = true;
        public static Boolean runOverlay = false;

        public static Boolean requestRefresh = false;

        public static DateTime startlights;

        public static ConnectionState apiState;
    }
}
