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

            public int userId;
            public int carId;

            public Boolean onTrack;

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

            public int driverFollowed;
        }

        public enum overlayObjects
        {
            driver,
            sidepanel,
            results,
            replay,
            sessionstate
        }

        public enum sidepanelTypes
        {
            leader,
            followed,
            fastlap
        }

        // Driver
        public static Mutex driversMutex;
        //public static AutoResetEvent driversMutexEvent = new AutoResetEvent(false);
        public static Driver[] drivers = new Driver[iRacingTelem.MAX_CARS];

        // LapInfo
        public static Mutex standingMutex;
        //public static AutoResetEvent standingMutexEvent = new AutoResetEvent(false);
        public static LapInfo[][] standing = new LapInfo[iRacingTelem.MAX_SESSIONS][];

        // SessionInfo
        public static Mutex sessionsMutex;
        //public static AutoResetEvent sessionsMutexEvent = new AutoResetEvent(false);
        public static SessionInfo[] sessions = new SessionInfo[iRacingTelem.MAX_SESSIONS];
        public static int currentSession;

        // States
        public static Boolean[] visible = new Boolean[Enum.GetValues(typeof(overlayObjects)).Length];
        //public static Boolean sidepanelVisible = false;
        //public static Boolean resultsVisible = false;
        public static sidepanelTypes sidepanelType = sidepanelTypes.leader;  // 0 = leader, 1 = followed

        public static int resultPage = -1;
        public static Boolean resultLastPage = false;
        public static int resultSession = -1;

        public static Boolean runApi = true;
        public static Boolean runOverlay = false;

        public static Boolean requestRefresh = false;
    }
}
