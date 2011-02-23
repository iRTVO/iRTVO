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

            public float fastestlap;
            public float previouslap;
            public int completedlaps;
            public DateTime lastNewLap;
            public int lastNewLapNr;

            public int userId;
            public int carId;
            public int numberPlate;

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

        public struct TrackInfo
        {
            public string name;
            public int id;
            public float length;
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
        public static Boolean[] lastPage;

        // allow retirement
        public static Boolean allowRetire = false;
    }
}
