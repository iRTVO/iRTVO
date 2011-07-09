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
using iRSDKSharp;
using System.ComponentModel;

namespace iRTVO
{
    public enum eventType
    {
        bookmark,
        session,
        offtrack,
        fastlap
    }

    public class Event: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        eventType type;
        DateTime timestamp;
        Int32 replaypos;
        String description;
        DriverInfo driver;
        SessionInfo.sessionType session;
        Int32 lapnum;

        public Event(eventType type, Int32 replay, DriverInfo driver, String desc, SessionInfo.sessionType session, Int32 lap)
        {
            this.type = type;
            this.timestamp = DateTime.Now;
            this.replaypos = replay;
            this.driver = driver;
            this.description = desc;
            this.session = session;
            this.lapnum = lap;
        }

        public String Session { get { return this.session.ToString(); } set { } }
        public DateTime Timestamp { get { return this.timestamp; } set {  } }
        public Int32 ReplayPos { get { return this.replaypos; } set {  } }
        public String Description { get { return this.description; } set {  } }
        public DriverInfo Driver { get { return this.driver; } set { } }
        public eventType Type { get { return this.type; } set { } }
        public Int32 Lap { get { return this.lapnum; } set { } }
    }

    public class Events : INotifyPropertyChanged
    {
        List<Event> list;

        public Events() 
        {
            list = new List<Event>();
        }

        public List<Event> List { get { return this.list; } set { this.list = value; this.NotifyPropertyChanged("List"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class Bookmarks : INotifyPropertyChanged
    {
        List<Event> list;

        public Bookmarks()
        {
            list = new List<Event>();
        }

        public List<Event> List { get { return this.list; } set { this.list = value; this.NotifyPropertyChanged("List"); } }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class CameraInfo// : INotifyPropertyChanged
    {
        /*
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        */
        public CameraGroup FindId(int id)
        {
            foreach (CameraGroup group in groups)
            {
                if (group.Id == id)
                {
                    return group;
                }
            }
            return new CameraGroup();
        }

        int currentgroup;
        int wantedgroup;
        BindingList<CameraGroup> groups;
        DateTime updated;

        public CameraInfo()
        {
            currentgroup = 0;
            wantedgroup = 0;
            groups = new BindingList<CameraGroup>();
            updated = DateTime.Now;
        }

        public int CurrentGroup { get { return currentgroup; } set { currentgroup = value; /*this.NotifyPropertyChanged("CurrentGroup");*/ } }
        public int WantedGroup { get { return wantedgroup; } set { wantedgroup = value; } }
        public BindingList<CameraGroup> Groups { get { return groups; } set { groups = value; updated = DateTime.Now; /*this.NotifyPropertyChanged("Groups");*/ } }
        public DateTime Updated { get { return updated; } set { } }

    }

    public class CameraGroup// : INotifyPropertyChanged
    {
        /*
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        */
        string name;
        int id;

        public CameraGroup()
        {
            name = "";
            id = -1;
        }

        public string Name { get { return name; } set { name = value; /*this.NotifyPropertyChanged("Name");*/ } }
        public int Id { get { return id; } set { id = value; /*this.NotifyPropertyChanged("Id");*/ } }
    }

    public class DriverInfo
    {
        string name;
        string initials;
        string shortname;

        string club;
        string car;
        string numberPlate;
        string license;

        int caridx;
        int userId;
        int carId;
        int carclass;

        Boolean onTrack;
        DateTime offTrackSince;

        public DriverInfo()
        {
            name = "";
            initials = "";
            shortname = "";

            club = "";
            car = "";
            carclass = 0;
            license = "";

            caridx = -1;
            userId = 0;
            carId = 0;
            numberPlate = "";
        }

        public string Name { get { return name; } set { name = value; } }
        public string Initials { get { return initials; } set { initials = value; } }
        public string Shortname { get { return shortname; } set { shortname = value; } }

        public string Club { get { return club; } set { club = value; } }
        public string Car { get { return car; } set { car = value; } }
        public string NumberPlate { get { return numberPlate; } set { numberPlate = value; } }
        public string License { get { return license; } set { license = value; } }

        public int CarIdx { get { return caridx; } set { caridx = value; } }
        public int UserId { get { return userId; } set { userId = value; } }
        public int CarId { get { return carId; } set { carId = value; } }
        public int CarClass { get { return carclass; } set { carclass = value; } }
    }

    public class LapInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        Int32 lapnum;
        Single laptime;
        Int32 position;
        Single gap;
        Int32 gaplaps;
        Single inteval;
        Int32 intervallaps;
        DateTime begin;
        List<Sector> sectortimes;

        public LapInfo() {
            lapnum = 0;
            laptime = 0;
            position = 0;
            gap = 0;
            gaplaps = 0;
            inteval = 0;
            intervallaps = 0;
            begin = DateTime.Now;
            sectortimes = new List<Sector>();
        }

        public Int32 LapNum { get { return lapnum; } set { lapnum = value;} }
        public Single LapTime { get { if (laptime == float.MaxValue) return 0.0f; else { return laptime; } } set { laptime = value; } }
        public Int32 Position { get { return position; } set { position = value; } }
        public Single Gap { get { if (gap == float.MaxValue) return 0; else { return gap; } } set { gap = value; } }
        public Int32 GapLaps { get { return gaplaps; } set { gaplaps = value; } }
        public Single Interval { get { if (position <= 1) return 0.0f; else { return inteval; } } set { inteval = value; } }
        public Int32 IntervalLaps { get { return intervallaps; } set { intervallaps = value; } }
        public DateTime Begin { get { return begin; } set { begin = value; } }
        public List<Sector> SectorTimes { get { return sectortimes; } set { sectortimes = value; } }

        // combined Gap and GapLaps
        public string Gap_HR { 
            get {
                if (gaplaps > 0)
                    return gaplaps + " L";
                else if (gap == float.MaxValue)
                    return "-.--";
                else
                    return gap.ToString("0.000");
            } set { } 
        }
    }

    public enum SurfaceType
    {
        NotInWorld = -1,
        OffTrack,
        InPitStall,
        AproachingPits,
        OnTrack
    };

    public class Sector
    {
        Int32 num;
        Single time;

        public Sector()
        {
            num = 0;
            time = 0;
        }

        public Int32 Num { get { return num; } set { num = value; } }
        public Single Time { get { return time; } set { time = value; } }
    }

    public class StandingsItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        DriverInfo driver;
        List<LapInfo> laps;
        Single fastestlap;
        Int32 lapsled;
        SurfaceType surface;
        //Boolean ontrack;
        //DateTime offtracksince;
        Double trackpct;
        Single speed;
        Double prevspeed;
        Int32 position;
        /*
        DateTime currentlapbegin;
        Single unofficiallaptime;
        */
        LapInfo currentlap;
        Int32 sector;
        DateTime sectorbegin;

        public StandingsItem()
        {
            driver = new DriverInfo();
            laps = new List<LapInfo>();
            fastestlap = 0;
            lapsled = 0;
            surface = SurfaceType.NotInWorld;
            //ontrack = true;
            //offtracksince = new DateTime();
            trackpct = 0;
            speed = 0;
            prevspeed = 0;
            position = 0;
            /*
            currentlapbegin = DateTime.Now;
            unofficiallaptime = 0;
             * */
            currentlap = new LapInfo();
            sector = 0;
            sectorbegin = DateTime.Now;
        }

        public LapInfo FindLap(Int32 num) {
            foreach (LapInfo lap in laps) {
                if (lap.LapNum == num)
                {
                    return lap;
                }
            }
            return new LapInfo();
        }

        public DriverInfo Driver { get { return driver; } set {  } }
        public List<LapInfo> Laps { get { return laps;  } set { laps = value; } }
        public Single FastestLap { get { if (fastestlap != Single.MaxValue) return fastestlap; else return 0; } set { fastestlap = value; } }
        public Int32 LapsLed { get { return lapsled; } set { lapsled = value; } }
        //public Boolean OnTrack { get { return ontrack; } set { ontrack = value; } }
        //public DateTime OffTrackSince { get { return offtracksince; } set { offtracksince = value; } }
        //public DateTime CurrentLapBegin { get { return currentlapbegin; } set { currentlapbegin = value; } }
        public SurfaceType TrackSurface { get { return surface; } set { surface = value; } }
        //public Single UnofficialLapTime { get { return unofficiallaptime; } set { unofficiallaptime = value; } }
        public LapInfo CurrentLap { get { return currentlap; } set { currentlap = value; } }
        public Int32 Sector { get { return sector; } set { sector = value; } }
        public DateTime SectorBegin { get { return sectorbegin; } set { sectorbegin = value; } }

        //public Int32 CurrentLapNum { get { if (trackpct < 0) return 0; else return (Int32)Math.Floor(trackpct); } set { } }
        public Double CurrentTrackPct { 
            get 
            { 
                if (trackpct > 0) 
                    return trackpct; 
                else 
                    return PreviousLap.LapNum; 
            } 
            set 
            {
                if (value > 0)
                {
                    trackpct = value;
                    currentlap.LapNum = (Int32)Math.Floor(value);
                }
                else
                {
                    speed = 0;
                    trackpct = 0;
                }
            }
        }

        public Single Speed { // meters per second
            get 
            { 
                if (speed > 0) 
                    return speed;
                else 
                    return 0; 
            }
            set { speed = value; } 
        }

        public Int32 Speed_kph
        {
            get
            {
                if (speed > 0)
                    return (Int32)(speed * 3.6);
                else
                    return 0;
            }
            set { speed = value; }
        }

        public Double Prevspeed { get { return prevspeed; } set { prevspeed = value; } }
        public int Position { get { return position; } set { position = value; } }

        public Double IntervalLive
        {
            get
            {
                if (position > 1 && speed > 0)
                {
                    return ((SharedData.Sessions.CurrentSession.FindPosition(position - 1).CurrentTrackPct - this.CurrentTrackPct) * SharedData.Track.length) / speed;
                }
                else
                {
                    return 0;
                }
            }
            set { }
        }

        public String IntervalLive_HR
        {
            get
            {
                if (IntervalLive == 0)
                {
                    return "-.--";
                }
                else if (currentlap.LapNum < SharedData.Sessions.CurrentSession.FindPosition(position - 1).CurrentLap.LapNum)
                {
                    return (SharedData.Sessions.CurrentSession.FindPosition(position - 1).CurrentLap.LapNum - currentlap.LapNum) + "L";
                }
                else
                {
                    return IntervalLive.ToString("0.0");
                }
            }
            set { }
        }

        public String GapLive_HR
        {
            get
            {
                if (GapLive == 0)
                {
                    return "-.--";
                }
                else if (currentlap.LapNum < SharedData.Sessions.CurrentSession.FindPosition(position - 1).CurrentLap.LapNum)
                {
                    return (SharedData.Sessions.CurrentSession.FindPosition(position - 1).CurrentLap.LapNum - currentlap.LapNum) + "L";
                }
                else
                {
                    return GapLive.ToString("0.0");
                }
            }
            set { }
        }

        public Double GapLive
        {
            get
            {
                if (position > 1 && speed > 0)
                {
                    StandingsItem leader = SharedData.Sessions.CurrentSession.getLeader();
                    return ((leader.CurrentTrackPct - this.CurrentTrackPct) * SharedData.Track.length) / speed;
                }
                else
                {
                    return 0;
                }
            }
            set { }
        }

        public LapInfo PreviousLap { get {
            int count = (Int32)Math.Floor(trackpct) - 1;
            if (count > 1)
                if (this.laps.Exists(l => l.LapNum.Equals(count)))
                    return this.FindLap(count);
                else
                    return this.FindLap(count-1);
            else if (count == 1)
                return laps[0];
            else
                return new LapInfo();
        } set { } }

        /*
        public LapInfo CurrentLap { get {
            int count = (Int32)Math.Floor(trackpct) - 1;
            if (count > 0)
                return laps[count - 1];
            else
                return new LapInfo();
        } set { } }
        */

        public void setDriver(int carIdx)
        {
            int index = SharedData.Drivers.FindIndex(d => d.CarIdx.Equals(carIdx));
            if (index >= 0)
            {
                driver = SharedData.Drivers[index];
            }
            else
            {
                driver = new DriverInfo();
            }
        }

        public void NotifyLaps()
        {
            this.NotifyPropertyChanged("Laps"); 
            this.NotifyPropertyChanged("PreviousLap");
            this.NotifyPropertyChanged("CurrentLap"); 
        }

        public void NotifySelf()
        {
            this.NotifyPropertyChanged("Driver");
            this.NotifyPropertyChanged("PreviousLap");
            this.NotifyPropertyChanged("FastestLap");
            this.NotifyPropertyChanged("LapsLed");
        }

        public void NotifyPosition()
        {
            this.NotifyPropertyChanged("Speed_kph");
            this.NotifyPropertyChanged("IntervalLive_HR");
            this.NotifyPropertyChanged("GapLive_HR");
            this.NotifyPropertyChanged("Position");

            this.NotifyPropertyChanged("Sector");

        }
    }

    public class SessionInfo : INotifyPropertyChanged
    {

        public enum sessionType
        {
            invalid,
            practice,
            qualify,
            race
        }

        public enum sessionState
        {
            invalid,
            gridding,
            warmup,
            pacing,
            racing,
            checkered,
            cooldown
        }

        public enum sessionFlag
        {
            // global flags
            checkered,
            white,
            green,
            yellow,
            red,
            blue,
            debris,
            crossed,
            yellowWaving,
            oneLapToGreen,
            greenHeld,
            tenToGo,
            fiveToGo,
            randomWaving,
            caution,
            cautionWaving,

            // drivers black flags
            black,
            disqualify,
            servicible, // car is allowed service (not a flag)
            furled,
            repair,

            // start lights
            startHidden,
            startReady,
            startSet,
            startGo,

        };

        public enum sessionStartLight
        {
            off,    // hidden
            ready,  // off
            set,    // red
            go      // green
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public StandingsItem FindPosition(int pos)
        {
            foreach (StandingsItem stand in standings)
            {
                if (stand.Position == pos)
                {
                    return stand;
                }
            }
            return new StandingsItem();
        }

        public StandingsItem FindDriver(int caridx)
        {
            foreach (StandingsItem stand in standings)
            {
                if (stand.Driver.CarIdx == caridx)
                {
                    return stand;
                }
            }
            return new StandingsItem();
        }

        Int32 id;
        Int32 lapsTotal;
        Int32 lapsComplete;
        Int32 leadChanges;
        Int32 cautions;
        Int32 cautionLaps;

        Single fastestlap;
        Double time;
        Double sessionlength;
        Double sessionstarttime;
        Int32 sessionstartpos;

        sessionType type;
        sessionState state;
        sessionFlag flag;
        sessionStartLight startlight;

        StandingsItem followedDriver;
        List<StandingsItem> standings;

        public SessionInfo()
        {
            id = 0;
            lapsTotal = 0;
            lapsComplete = 0;
            leadChanges = 0;
            cautions = 0;
            cautionLaps = 0;

            fastestlap = 0;
            time = 0;
            sessionlength = 0;
            sessionstarttime = -1;
            sessionstartpos = 0;

            type = sessionType.invalid;
            state = sessionState.invalid;
            flag = sessionFlag.green;
            startlight = sessionStartLight.off;

            standings = new List<StandingsItem>();
            followedDriver = new StandingsItem();
        }

        public int Id { get { return id; } set { id = value; this.NotifyPropertyChanged("Id"); } }
        public int LapsTotal { 
            get 
            { 
                if(lapsTotal >= Int32.MaxValue) return 0; 
                else return lapsTotal; 
            } 
            set 
            { 
                lapsTotal = value; 
                this.NotifyPropertyChanged("LapsTotal"); 
                this.NotifyPropertyChanged("LapsRemaining"); 
            } 
        }
        public int LapsComplete
        {
            get { 
                if (lapsComplete < 0) return 0; 
                else return lapsComplete;
            } 
            set { 
                lapsComplete = value; 
                this.NotifyPropertyChanged("LapsComplete");
                this.NotifyPropertyChanged("LapsRemaining"); 
            } 
        }
        public Int32 LapsRemaining { get { if((lapsTotal-lapsComplete) < 0) return 0; else return (lapsTotal-lapsComplete); } set { } }
        public Int32 LeadChanges { get { return leadChanges; } set { leadChanges = value; } }
        public Int32 Cautions { get { return cautions; } set { cautions = value; } }
        public Int32 CautionLaps { get { return cautionLaps; } set { cautionLaps = value; } }

        public Single FastestLap { get { return fastestlap; } set { fastestlap = value;  } }
        public Double SessionLength { get { return sessionlength; } set { sessionlength = value; } }
        public Double Time { get { return time; } set { time = value; } }
        public Double TimeRemaining { get { if (sessionlength >= Single.MaxValue) return 0; else return (sessionlength - time); } set { } }
        public Double SessionStartTime { get { return sessionstarttime; } set { sessionstarttime = value; } }
        public Int32 CurrentReplayPosition { get { return (Int32)((time - sessionstarttime) * 60) + sessionstartpos; } set { sessionstartpos = value; } }

        public sessionType Type { get { return type; } set { type = value; } }
        public sessionState State { get { return state; } set { state = value;  } }
        public sessionFlag Flag { get { return flag; } set { flag = value;  } }
        public sessionStartLight StartLight { get { return startlight; } set { startlight = value; } }

        public List<StandingsItem> Standings { get { return standings; } set { standings = value; } }

        public StandingsItem FollowedDriver { get { return followedDriver; } set { } }

        public void setFollowedDriver(Int32 carIdx)
        {
            StandingsItem stand = this.FindDriver(carIdx);
            if(stand.Driver.CarIdx >= 0)
            {
                followedDriver = stand;
            }
            else
            {
                followedDriver = new StandingsItem();
            }
        }

        public StandingsItem getLeader()
        {
            StandingsItem stand = this.FindPosition(1);
            if (stand.Driver.CarIdx >= 0)
            {
                return stand;
            }
            else
            {
                return new StandingsItem();
            }
        }

        public void UpdatePosition() 
        {
            Int32 i = 1;
            IEnumerable<StandingsItem> query;
            if (this.type == sessionType.race)
            {
                query = standings.OrderByDescending(s => s.CurrentTrackPct);
            }
            else
            {
                query = standings.OrderBy(s => s.FastestLap);
            }

            foreach (StandingsItem si in query)
            {
                si.Position = i++;
                si.NotifyPosition();
                //i++;
            }
        }
    }

    

    public class Sessions
    {
        List<SessionInfo> sessions;
        int currentsession;
        int sessionid;
        int subsessionid;

        public Sessions()
        {
            sessions = new List<SessionInfo>();
            currentsession = 0;
            sessionid = 0;
            subsessionid = 0;
        }

        public List<SessionInfo> SessionList { get { return sessions; } set { sessions = value; } }
        public SessionInfo CurrentSession { get { if (sessions.Count > 0) return sessions[currentsession]; else return new SessionInfo(); } set { } }
        public int SessionId { get { return sessionid; } set { sessionid = value; } }
        public int SubSessionId { get { return subsessionid; } set { subsessionid = value; } }

        public void setCurrentSession(int id)
        {
            int index = sessions.FindIndex(s => s.Id.Equals(id));
            if (index >= 0)
            {
                currentsession = index;
            }
            else
            {
                currentsession = 0;
            }
        }
        
    }

    public struct TrackInfo
    {
        public String name;
        public Int32 id;
        public Single length;
    }

    class SharedData
    {
        // Mutexes
        public static Mutex mutex = new Mutex(false);

        // API state
        public static Boolean runApi = true;
        public static Boolean runOverlay = false;
        public static Boolean apiConnected = false;

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
        //public static Boolean[] webUpdateWait = new Boolean[Enum.GetValues(typeof(webTiming.postTypes)).Length];
        public static Int64 webBytes = 0;
        public static String webError;

        // Data
        public static List<DriverInfo> Drivers = new List<DriverInfo>();
        public static Sessions Sessions = new Sessions();
        public static TrackInfo Track = new TrackInfo();
        public static CameraInfo Camera = new CameraInfo();
        public static Events Events = new Events();
        public static Bookmarks Bookmarks = new Bookmarks();
        public static List<Single> Sectors = new List<Single>();
        public static List<Single> SelectedSectors = new List<Single>();

        // Update stuff
        public static Boolean updateControls = false;

        /*
        public static int currentSession = 0;
        public static int sessionId = 0;
        public static int subSessionId = 0;
        */
    }
}
