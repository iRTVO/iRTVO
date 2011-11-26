using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// additional
using System.ComponentModel;

namespace iRTVO {

    public class Event : INotifyPropertyChanged
    {

        public enum eventType
        {
            bookmark,
            offtrack,
            fastlap,
            pit,
            flag,
            state,
            startlights
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        eventType type;
        DateTime timestamp;
        Int64 replaypos;
        String description;
        DriverInfo driver;
        Sessions.SessionInfo.sessionType session;
        Int32 lapnum;

        public Event(eventType type, Int64 replay, DriverInfo driver, String desc, Sessions.SessionInfo.sessionType session, Int32 lap)
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
        public DateTime Timestamp { get { return this.timestamp; } set { } }
        public Int64 ReplayPos { get { return this.replaypos; } set { } }
        public String Description { get { return this.description; } set { } }
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

    public class CameraInfo
    {
        public class CameraGroup
        {

            string name;
            int id;

            public CameraGroup()
            {
                name = "";
                id = -1;
            }

            public string Name { get { return name; } set { name = value;} }
            public int Id { get { return id; } set { id = value; } }
        }

        public CameraGroup FindId(int id)
        {
            int index = groups.FindIndex(g => g.Id.Equals(id));
            if (index >= 0)
            {
                return groups[index];
            }
            else
            {
                return new CameraGroup();
            }
        }

        int currentgroup;
        int wantedgroup;
        List<CameraGroup> groups;
        DateTime updated;

        public CameraInfo()
        {
            currentgroup = 0;
            wantedgroup = 0;
            groups = new List<CameraGroup>();
            updated = DateTime.Now;
        }

        public int CurrentGroup { get { return currentgroup; } set { currentgroup = value; /*this.NotifyPropertyChanged("CurrentGroup");*/ } }
        public int WantedGroup { get { return wantedgroup; } set { wantedgroup = value; } }
        public List<CameraGroup> Groups { get { return groups; } set { groups = value; updated = DateTime.Now; /*this.NotifyPropertyChanged("Groups");*/ } }
        public DateTime Updated { get { return updated; } set { } }

    }

    public class DriverInfo
    {
        string name;
        string initials;
        string shortname;

        string club;
        string sr;
        string numberPlate;

        int caridx;
        int userId;
        int carId;
        int carclass;

        public DriverInfo()
        {
            name = "";
            initials = "";
            shortname = "";

            club = "";
            sr = "";
            carclass = 0;

            caridx = -1;
            userId = 0;
            carId = 0;
            numberPlate = "0";
        }

        public string Name { get { return name; } set { name = value; } }
        public string Initials { get { return initials; } set { initials = value; } }
        public string Shortname { get { return shortname; } set { shortname = value; } }

        public string Club { get { return club; } set { club = value; } }
        public string SR { get { return sr; } set { sr = value; } }
        public string NumberPlate { get { return numberPlate; } set { numberPlate = value; } }

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

        public class Sector
        {
            Int32 num;
            Single time;
            Single speed;
            Double begin;

            public Sector()
            {
                num = 0;
                time = 0;
                speed = 0;
                begin = 0;
            }

            public Int32 Num { get { return num; } set { num = value; } }
            public Single Time { get { return time; } set { time = value; } }
            public Single Speed { get { return speed; } set { speed = value; } }
            public Double Begin { get { return begin; } set { begin = value; } }
        }

        Int32 lapnum;
        Single laptime;
        Int32 position;
        Single gap;
        Int32 gaplaps;
        List<Sector> sectortimes;

        public LapInfo()
        {
            lapnum = 0;
            laptime = 0;
            position = 0;
            gap = 0;
            gaplaps = 0;
            sectortimes = new List<Sector>(3);
        }

        public Int32 LapNum { get { return lapnum; } set { lapnum = value; } }
        public Single LapTime { get { if (laptime == float.MaxValue) return 0.0f; else { return laptime; } } set { laptime = value; } }
        public Int32 Position { get { return position; } set { position = value; } }
        public Single Gap { get { if (gap == float.MaxValue) return 0; else { return gap; } } set { gap = value; } }
        public Int32 GapLaps { get { return gaplaps; } set { gaplaps = value; } }
        
        public List<Sector> SectorTimes { get { return sectortimes; } set { sectortimes = value; } }

        // combined Gap and GapLaps
        public string Gap_HR
        {
            get
            {
                if (gaplaps > 0)
                    return gaplaps + " L";
                else if (gap == float.MaxValue)
                    return "-.--";
                else
                    return gap.ToString("0.000");
            }
            set { }
        }
    }

    public class Sessions
    {
        public class SessionInfo : INotifyPropertyChanged
        {

            public class StandingsItem : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler PropertyChanged;

                private void NotifyPropertyChanged(string name)
                {
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs(name));
                }

                public enum SurfaceType
                {
                    NotInWorld = -1,
                    OffTrack,
                    InPitStall,
                    AproachingPits,
                    OnTrack
                };

                DriverInfo driver;
                List<LapInfo> laps;
                Single fastestlap;
                Int32 lapsled;
                SurfaceType surface;
                Double trackpct;
                Double prevtrackpct;
                Single speed;
                Double prevspeed;
                Int32 position;
                LapInfo currentlap;
                Int32 sector;
                Double sectorbegin;
                Int32 pitstops;
                Single pitstoptime;
                DateTime pitstorbegin;
                Double begin;
                Boolean finished;
                DateTime offtracksince;

                public StandingsItem()
                {
                    driver = new DriverInfo();
                    laps = new List<LapInfo>();
                    fastestlap = 0;
                    lapsled = 0;
                    surface = SurfaceType.NotInWorld;
                    trackpct = 0;
                    prevtrackpct = 0;
                    speed = 0;
                    prevspeed = 0;
                    position = 0;
                    currentlap = new LapInfo();
                    sector = 0;
                    sectorbegin = 0;
                    pitstops = 0;
                    pitstoptime = 0;
                    pitstorbegin = DateTime.MinValue;
                    begin = 0;
                    finished = false;
                    offtracksince = DateTime.MinValue;
                }

                public LapInfo FindLap(Int32 num)
                {
                    int index = laps.FindIndex(f => f.LapNum.Equals(num));
                    if (index >= 0)
                        return laps[index];
                    else
                        return new LapInfo();
                }

                public DriverInfo Driver { get { return driver; } set { } }
                public List<LapInfo> Laps { get { return laps; } set { laps = value; } }
                public Single FastestLap { get { if (fastestlap != Single.MaxValue) return fastestlap; else return 0; } set { fastestlap = value; } }
                public Int32 LapsLed { get { return lapsled; } set { lapsled = value; } }
                public SurfaceType TrackSurface { get { return surface; } set { surface = value; } } 
                public Int32 Sector { get { return sector; } set { sector = value; } }
                public Double SectorBegin { get { return sectorbegin; } set { sectorbegin = value; } }
                public Int32 PitStops { get { return pitstops; } set { pitstops = value; } }
                public Single PitStopTime { get { return pitstoptime; } set { pitstoptime = value; } }
                public DateTime PitStopBegin { get { return pitstorbegin; } set { pitstorbegin = value; } }
                public Double Begin { get { return begin; } set { begin = value; } }
                public Boolean Finished { get { return finished; } set { finished = value; } }
                public DateTime OffTrackSince { get { return offtracksince; } set { offtracksince = value; } }
                public Double PrevTrackPct { get { return prevtrackpct; } set { prevtrackpct = value; } }

                public LapInfo CurrentLap 
                { 
                    get 
                    {
                        if (surface == SurfaceType.NotInWorld && finished == false)
                            return PreviousLap;
                        else
                            return currentlap; 
                    } 
                    set { currentlap = value; } 
                }

                public Double CurrentTrackPct
                {
                    get
                    {
                        if (trackpct > 0)
                            return trackpct;
                        else
                            return PreviousLap.LapNum;
                    }
                    set
                    {
                        trackpct = value;
                        currentlap.LapNum = (Int32)Math.Floor(value);
                    }
                }

                public Single Speed
                { 
                    // meters per second
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
                        if (position > 1 && speed > 1)
                        {
                            /*
                            if(this.driver.CarIdx == SharedData.Sessions.CurrentSession.FollowedDriver.Driver.CarIdx)
                                Console.WriteLine(((SharedData.Sessions.CurrentSession.FindPosition(position - 1).CurrentTrackPct - this.CurrentTrackPct) * SharedData.Track.length) / speed + " s:"+ speed);
                            */
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
                        else if ((SharedData.Sessions.CurrentSession.FindPosition(this.position - 1).CurrentTrackPct - trackpct) > 1)
                        {
                            return (SharedData.Sessions.CurrentSession.FindPosition(this.position - 1).CurrentLap.LapNum - currentlap.LapNum) + "L";
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
                        else if ((SharedData.Sessions.CurrentSession.getLeader().CurrentTrackPct - CurrentTrackPct) > 1)
                        {
                            return (SharedData.Sessions.CurrentSession.getLeader().CurrentLap.LapNum - currentlap.LapNum) + "L";
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
                        if (position > 1 && speed > 1)
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

                public LapInfo PreviousLap
                {
                    get
                    {
                        if (finished == true)
                        {
                            return currentlap;
                        }
                        else
                        {
                            int count = (Int32)Math.Floor(trackpct);
                            if (count > 1)
                            {
                                if (this.laps.Exists(l => l.LapNum.Equals(count)))
                                    return this.FindLap(count);
                                else
                                    return this.FindLap(count - 1);
                            }
                            else if (count == 1 && laps.Count == 1)
                            {
                                return laps[0];
                            }
                            else
                            {
                                return new LapInfo();
                            }
                        }
                    }
                    set { }
                }

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
                        Console.WriteLine("Driver for caridx "+ carIdx +" not found");
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
                public void NotifyPit()
                {
                    this.NotifyPropertyChanged("PitStops");
                    this.NotifyPropertyChanged("PitStopTime");
                }
            }

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
                int index = standings.FindIndex(f => f.Position.Equals(pos));
                if (index >= 0)
                    return standings[index];
                else
                    return new StandingsItem();
            }

            public StandingsItem FindDriver(int caridx)
            {
                int index = standings.FindIndex(s => s.Driver.CarIdx.Equals(caridx));
                if (index >= 0)
                {
                    return standings[index];
                }
                else
                {
                    return new StandingsItem();
                    
                }
            }

            Int32 id;
            Int32 lapsTotal;
            Int32 lapsComplete;
            Int32 leadChanges;
            Int32 cautions;
            Int32 cautionLaps;

            Single fastestlap;
            DriverInfo fastestdriver;
            Int32 fastestlapnum;

            Double time;
            Double sessionlength;
            Double sessionstarttime;
            Int32 sessionstartpos;
            Int32 finishline;

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
                fastestdriver = new DriverInfo();
                fastestlapnum = 0;

                time = 0;
                sessionlength = 0;
                sessionstarttime = -1;
                sessionstartpos = 0;
                finishline = Int32.MaxValue;

                type = sessionType.invalid;
                state = sessionState.invalid;
                flag = sessionFlag.green;
                startlight = sessionStartLight.off;

                standings = new List<StandingsItem>();
                followedDriver = new StandingsItem();
            }

            public int Id { get { return id; } set { id = value; this.NotifyPropertyChanged("Id"); } }
            public int LapsTotal
            {
                get
                {
                    if (lapsTotal >= Int32.MaxValue) return 0;
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
                get
                {
                    if (lapsComplete < 0) return 0;
                    else return lapsComplete;
                }
                set
                {
                    lapsComplete = value;
                    this.NotifyPropertyChanged("LapsComplete");
                    this.NotifyPropertyChanged("LapsRemaining");
                }
            }
            public Int32 LapsRemaining { get { if ((lapsTotal - lapsComplete) < 0) return 0; else return (lapsTotal - lapsComplete); } set { } }
            public Int32 LeadChanges { get { return leadChanges; } set { leadChanges = value; } }
            public Int32 Cautions { get { return cautions; } set { cautions = value; } }
            public Int32 CautionLaps { get { return cautionLaps; } set { cautionLaps = value; } }

            public Single FastestLap { get { return fastestlap; } set { fastestlap = value; } }
            public DriverInfo FastestLapDriver { get { return fastestdriver; } set { fastestdriver = value; } }
            public Int32 FastestLapNum { get { return fastestlapnum; } set { fastestlapnum = value; } }

            public Double SessionLength { get { return sessionlength; } set { sessionlength = value; } }
            public Double Time { get { return time; } set { time = value; } }
            public Double TimeRemaining { get { if (sessionlength >= Single.MaxValue) return 0; else return (sessionlength - time); } set { } }
            public Double SessionStartTime { get { return sessionstarttime; } set { sessionstarttime = value; } }
            public Int32 CurrentReplayPosition { get { return (Int32)((time - sessionstarttime) * 60) + sessionstartpos; } set { sessionstartpos = value; } }
            public Int32 FinishLine { get { return finishline; } set { finishline = value; } }

            public sessionType Type { get { return type; } set { type = value; } }
            public sessionState State { get { return state; } set { state = value; } }
            public sessionFlag Flag { get { return flag; } set { flag = value; } }
            public sessionStartLight StartLight { get { return startlight; } set { startlight = value; } }

            public List<StandingsItem> Standings { get { return standings; } set { standings = value; } }

            public StandingsItem FollowedDriver { get { return followedDriver; } set { } }

            public void setFollowedDriver(Int32 carIdx)
            {
                followedDriver = FindDriver(carIdx);
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
                    foreach (StandingsItem si in query)
                    {
                        si.Position = i++;
                        si.NotifyPosition();
                    }
                }
                else
                {
                    query = standings.OrderBy(s => s.FastestLap);
                    foreach (StandingsItem si in query)
                    {
                        if (si.FastestLap > 0) // skip driver without time
                        {
                            si.Position = i++;
                            si.NotifyPosition();
                        }
                    }
                }
            }
        }

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
        public Int32 id;
        public Single length;
        public String name;
    }
}
