using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// additional
using System.Globalization;
using System.ComponentModel;
using System.Xml.Serialization;
using Ini;
using System.IO;


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
        Int32 rewind;

        public Event(eventType type, Int64 replay, DriverInfo driver, String desc, Sessions.SessionInfo.sessionType session, Int32 lap)
        {
            this.type = type;
            this.timestamp = DateTime.Now;
            this.replaypos = replay;
            this.driver = driver;
            this.description = desc;
            this.session = session;
            this.lapnum = lap;
            this.rewind = 0;
        }

        public String Session { get { return this.session.ToString(); } set { } }
        public DateTime Timestamp { get { return this.timestamp; } set { } }
        public Int64 ReplayPos { get { return this.replaypos; } set { } }
        public String Description { get { return this.description; } set { } }
        public DriverInfo Driver { get { return this.driver; } set { } }
        public eventType Type { get { return this.type; } set { } }
        public Int32 Lap { get { return this.lapnum; } set { } }
        public Int32 Rewind { get { return this.rewind; } set { this.rewind = value; } }
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
        string carclassname;

        int irating;
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
            carclassname = "";

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
        public int iRating { get { return irating; } set { irating = value; } }
        public string NumberPlate { get { return numberPlate; } set { numberPlate = value; } }
        public int NumberPlateInt { get { if (numberPlate != null) return Int32.Parse(numberPlate); else return 0; } }
        public int CarIdx { get { return caridx; } set { caridx = value; } }
        public int UserId { get { return userId; } set { userId = value; } }
        public int CarId { get { return carId; } set { carId = value; } }
        public int CarClass { get { return carclass; } set { carclass = value; } }
        public string CarClassName { get { return carclassname; } set { carclassname = value; } }
        public int CarClassOrder { 
            get {
                if (SharedData.ClassOrder.ContainsKey(carclassname))
                    return SharedData.ClassOrder[carclassname] * 100;
                else
                    return 100;
            } 
            set { } 
        }
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
        Int32 replayPos;
        Double sessionTime;
        Int32 classposition;

        public LapInfo()
        {
            lapnum = 0;
            laptime = 0;
            position = 0;
            classposition = 0;
            gap = 0;
            gaplaps = 0;
            sectortimes = new List<Sector>(3);
            replayPos = 0;
        }

        public Int32 LapNum { get { return lapnum; } set { lapnum = value; } }
        public Single LapTime { get { if (laptime == float.MaxValue) return 0.0f; else { return laptime; } } set { laptime = value; } }
        public Int32 Position { get { return position; } set { position = value; } }
        public Int32 ClassPosition { get { return classposition; } set { classposition = value; } }
        public Single Gap { get { if (gap == float.MaxValue) return 0; else { return gap; } } set { gap = value; } }
        public Int32 GapLaps { get { return gaplaps; } set { gaplaps = value; } }
        public Int32 ReplayPos { get { return replayPos; } set { replayPos = value; } }
        public Double SessionTime { get { return sessionTime; } set { sessionTime = value; } }
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

    public class Sessions  : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

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
                Int32 classlapsled;
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
                Double offtracksince;
                Int32 positionlive;

                public StandingsItem()
                {
                    driver = new DriverInfo();
                    laps = new List<LapInfo>();
                    fastestlap = 0;
                    lapsled = 0;
                    classlapsled = 0;
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
                    offtracksince = 0;
                    positionlive = 0;
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
                public Int32 ClassLapsLed { get { return classlapsled; } set { classlapsled = value; } }
                public SurfaceType TrackSurface { get { return surface; } set { surface = value; } } 
                public Int32 Sector { get { return sector; } set { sector = value; } }
                public Double SectorBegin { get { return sectorbegin; } set { sectorbegin = value; } }
                public Int32 PitStops { get { return pitstops; } set { pitstops = value; } }
                public Single PitStopTime { get { return pitstoptime; } set { pitstoptime = value; } }
                public DateTime PitStopBegin { get { return pitstorbegin; } set { pitstorbegin = value; } }
                public Double Begin { get { return begin; } set { begin = value; } }
                public Boolean Finished { get { return finished; } set { finished = value; } }
                public Double OffTrackSince { get { return offtracksince; } set { offtracksince = value; } }
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
                    set { }
                }

                public Double Prevspeed { get { return prevspeed; } set { prevspeed = value; } }
                public int Position { get { return position; } set { position = value; } }
                public int PositionLive { get { return positionlive; } set { positionlive = value; } }

                public Double IntervalLive
                {
                    get
                    {
                        if (position > 1 && speed > 1)
                            return SharedData.timedelta.GetDelta(this.driver.CarIdx, SharedData.Sessions.CurrentSession.FindPosition(this.positionlive - 1, dataorder.liveposition).driver.CarIdx).TotalSeconds;
                        else
                        {
                            return 0;
                        }
                    }
                    set { }
                }


                public String IntervalLive_HR_rounded
                {
                    get
                    {
                        return this.IntervalLive_HR(1);
                    }
                    set { }
                }

                public String IntervalLive_HR(Int32 rounding)
                {
                    if (IntervalLive == 0)
                    {
                        return "-.--";
                    }
                    else if ((SharedData.Sessions.CurrentSession.FindPosition(this.positionlive - 1, dataorder.liveposition).CurrentTrackPct - trackpct) > 1)
                    {
                        return (SharedData.Sessions.CurrentSession.FindPosition(this.positionlive - 1, dataorder.liveposition).CurrentLap.LapNum - currentlap.LapNum) + "L";
                    }
                    else
                    {
                        return Theme.round(IntervalLive, rounding);
                    }
                }

                public String GapLive_HR_rounded
                {
                    get
                    {
                        return this.GapLive_HR(1);
                    }
                }
                public String GapLive_HR(Int32 rounding)
                {
                    if (GapLive == 0)
                    {
                        return "-.--";
                    }
                    else if ((SharedData.Sessions.CurrentSession.getLiveLeader().CurrentTrackPct - CurrentTrackPct) > 1)
                    {
                        return (SharedData.Sessions.CurrentSession.getLiveLeader().CurrentLap.LapNum - currentlap.LapNum) + "L";
                    }
                    else
                    {
                        return Theme.round(GapLive, rounding);
                    }
                }

                public String ClassIntervalLive_HR
                {
                    get
                    {
                        if (IntervalLive == 0)
                        {
                            return "-.--";
                        }
                        else if ((SharedData.Sessions.CurrentSession.FindPosition(this.positionlive - 1, dataorder.liveposition, this.driver.CarClassName).CurrentTrackPct - trackpct) > 1)
                        {
                            return (SharedData.Sessions.CurrentSession.FindPosition(this.positionlive - 1, dataorder.liveposition, this.driver.CarClassName).CurrentLap.LapNum - currentlap.LapNum) + "L";
                        }
                        else
                        {
                            return IntervalLive.ToString("0.0");
                        }
                    }
                    set { }
                }

                public String ClassGapLive_HR
                {
                    get
                    {
                        if (GapLive == 0)
                        {
                            return "-.--";
                        }
                        else if ((SharedData.Sessions.CurrentSession.getClassLeader(this.driver.CarClassName).CurrentTrackPct - CurrentTrackPct) > 1)
                        {
                            return (SharedData.Sessions.CurrentSession.getClassLeader(this.driver.CarClassName).CurrentLap.LapNum - currentlap.LapNum) + "L";
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
                        if (this.positionlive > 1 && this.speed > 1)
                        {
                            StandingsItem leader = SharedData.Sessions.CurrentSession.getLiveLeader();
                            return SharedData.timedelta.GetDelta(this.driver.CarIdx, leader.driver.CarIdx).TotalSeconds;
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

                public Int32 HighestPosition
                {
                    get
                    {
                        IEnumerable<LapInfo> result = laps.Where(l => l.Position > 0).OrderBy(l => l.Position);
                        if (result.Count() > 0)
                            return result.First().Position;
                        else
                            return 0;                    
                    }
                    set { }
                }

                public Int32 LowestPosition
                {
                    get
                    {
                        IEnumerable<LapInfo> result = laps.OrderByDescending(l => l.Position);
                        if (result.Count() > 0)
                            return result.First().Position;
                        else
                            return 0;
                    }
                    set { }
                }

                public Int32 HighestClassPosition
                {
                    get
                    {
                        IEnumerable<LapInfo> result = laps.Where(l => l.ClassPosition > 0).OrderBy(l => l.ClassPosition);
                        if (result.Count() > 0)
                            return result.First().ClassPosition;
                        else
                            return 0;
                    }
                    set { }
                }

                public Int32 LowestClassPosition
                {
                    get
                    {
                        IEnumerable<LapInfo> result = laps.OrderByDescending(l => l.ClassPosition);
                        if (result.Count() > 0)
                            return result.First().ClassPosition;
                        else
                            return 0;
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
                    this.NotifyPropertyChanged("Speed");
                    this.NotifyPropertyChanged("IntervalLive_HR_rounded");
                    this.NotifyPropertyChanged("GapLive_HR_rounded");
                    this.NotifyPropertyChanged("Gap_HR");
                    this.NotifyPropertyChanged("Position");
                    this.NotifyPropertyChanged("PositionLive");
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
                warmup, 
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

                // invalid
                invalid
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

            public StandingsItem FindPosition(int pos, dataorder order)
            {
                return this.FindPosition(pos, order, null);
            }

            public StandingsItem FindPosition(int pos, dataorder order, string classname)
            {
                int index = -1;
                int i = 1;
                IEnumerable<Sessions.SessionInfo.StandingsItem> query;
                switch (order)
                {
                   case dataorder.fastestlap:
                        Int32 lastpos = SharedData.Drivers.Count;

                        if(classname == null)
                            query = SharedData.Sessions.CurrentSession.Standings.OrderBy(s => s.FastestLap);
                        else
                            query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.Driver.CarClassName == classname).OrderBy(s => s.FastestLap);

                        foreach (Sessions.SessionInfo.StandingsItem si in query)
                        {
                            if (si.FastestLap > 0)
                            {
                                if (i == pos)
                                {
                                    index = standings.FindIndex(f => f.Driver.CarIdx.Equals(si.Driver.CarIdx));
                                    break;
                                }

                                i++;
                            }
                        }

                        // if not found then driver has no finished laps
                        if (index < 0)
                        {
                            if (classname == null)
                                query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.FastestLap <= 0);
                            else
                                query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.Driver.CarClassName == classname).Where(s => s.FastestLap <= 0);

                            foreach (Sessions.SessionInfo.StandingsItem si in query)
                            {                                    
                                if (i == pos)
                                {
                                    index = standings.FindIndex(f => f.Driver.CarIdx.Equals(si.Driver.CarIdx));
                                    break;
                                }

                                i++;                            
                            }
                        }
                        break;
                   case dataorder.previouslap:

                        if (classname == null)
                            query = SharedData.Sessions.CurrentSession.Standings.OrderBy(s => s.PreviousLap.LapTime);
                        else
                            query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.Driver.CarClassName == classname).OrderBy(s => s.PreviousLap.LapTime);
                        try
                        {
                            foreach (Sessions.SessionInfo.StandingsItem si in query)
                            {
                                if (si.PreviousLap.LapTime > 0)
                                {
                                    if (i == pos)
                                    {
                                        index = standings.FindIndex(f => f.Driver.CarIdx.Equals(si.Driver.CarIdx));
                                        break;
                                    }

                                    i++;
                                }
                            }
                        }
                        catch
                        {
                            index = -1;
                        }

                        // if not found then driver has no finished laps
                        if (index < 0)
                        {
                            if (classname == null)
                                query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.PreviousLap.LapTime <= 0);
                            else
                                query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.Driver.CarClassName == classname).Where(s => s.PreviousLap.LapTime <= 0);

                            foreach (Sessions.SessionInfo.StandingsItem si in query)
                            {
                                if (i == pos)
                                {
                                    index = standings.FindIndex(f => f.Driver.CarIdx.Equals(si.Driver.CarIdx));
                                    break;
                                }
                                i++;
                            }
                        }
                        break;
                   case dataorder.classposition:
                        query = SharedData.Sessions.CurrentSession.Standings.OrderBy(s => s.Driver.CarClassOrder + s.Position).Skip(pos - 1);
                        if (query.Count() > 0)
                        {
                            StandingsItem si = query.First();
                            return si;
                        }
                        else
                            return new StandingsItem();
                    case dataorder.points:
                        /*
                        query = SharedData.Sessions.CurrentSession.Standings.OrderByDescending(s => s.Points).Skip(pos - 1);
                        if (query.Count() > 0)
                        {
                            StandingsItem si = query.First();
                            return si;
                        }
                        else
                            return new StandingsItem();
                        */
                        return new StandingsItem();
                    case dataorder.liveposition:
                        if (classname == null)
                            index = standings.FindIndex(f => f.PositionLive.Equals(pos));
                        else
                        {
                            query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.Driver.CarClassName == classname).OrderBy(s => s.PositionLive).Skip(pos - 1);
                            if (query.Count() > 0)
                            {
                                StandingsItem si = query.First();
                                return si;
                            }
                            else
                                return new StandingsItem();
                        }
                        break;
                    default:
                        if (classname == null)
                            index = standings.FindIndex(f => f.Position.Equals(pos));
                        else
                        {
                            query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.Driver.CarClassName == classname).OrderBy(s => s.Position).Skip(pos-1);
                            if (query.Count() > 0)
                            {
                                StandingsItem si = query.First();
                                return si;
                            }
                            else
                                return new StandingsItem();
                        }
                        break;
                }
                 
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

            public Int32 getClassPosition(DriverInfo driver)
            {
                IEnumerable<Sessions.SessionInfo.StandingsItem> query = this.Standings.Where(s => s.Driver.CarClassName == driver.CarClassName).OrderBy(s => s.Position);
                Int32 position = 1;
                foreach (Sessions.SessionInfo.StandingsItem si in query)
                {
                    if (si.Driver.CarIdx == driver.CarIdx)
                        return position;
                    else
                        position++;
                }
                return 0;
            }

            public Int32 getClassLivePosition(DriverInfo driver)
            {
                IEnumerable<Sessions.SessionInfo.StandingsItem> query = this.Standings.Where(s => s.Driver.CarClassName == driver.CarClassName).OrderBy(s => s.PositionLive);
                Int32 position = 1;
                foreach (Sessions.SessionInfo.StandingsItem si in query)
                {
                    if (si.Driver.CarIdx == driver.CarIdx)
                        return position;
                    else
                        position++;
                }
                return 0;
            }

            public StandingsItem getClassLeader(string className)
            {
                if (className.Length > 0)
                {
                    IEnumerable<Sessions.SessionInfo.StandingsItem> query = this.Standings.Where(s => s.Driver.CarClassName == className).OrderBy(s => s.Position);
                    return query.First();
                }
                else
                    return new StandingsItem();
            }

            public Int32 getClassCarCount(string className)
            {
                IEnumerable<Sessions.SessionInfo.StandingsItem> query = this.Standings.Where(s => s.Driver.CarClassName == className);
                return query.Count();
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
            Double sessiontimeremaining;
            Double sessionlength;
            Double sessionstarttime;
            Int32 sessionstartpos;
            Int32 finishline;
            Boolean hosted;

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
                sessiontimeremaining = 0;
                sessionlength = 0;
                sessionstarttime = -1;
                sessionstartpos = 0;
                finishline = Int32.MaxValue;

                type = sessionType.invalid;
                state = sessionState.invalid;
                flag = sessionFlag.invalid;
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
            public Double TimeRemaining { get { return sessiontimeremaining; } set { sessiontimeremaining = value; } }
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
                StandingsItem stand = this.FindPosition(1, dataorder.position);
                if (stand.Driver.CarIdx >= 0)
                {
                    return stand;
                }
                else
                {
                    return new StandingsItem();
                }
            }

            public StandingsItem getLiveLeader()
            {
                StandingsItem stand = this.FindPosition(1, dataorder.liveposition);
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
                Int32 backmarker = standings.Count;
                Int32 i = 1;
                IEnumerable<StandingsItem> query;
                if (this.type == sessionType.race)
                {
                    query = standings.OrderByDescending(s => s.CurrentTrackPct);
                    foreach (StandingsItem si in query)
                    {
                        si.PositionLive = i++;
                        si.NotifyPosition();
                    }
                }
                else
                {
                    /*
                    query = standings.OrderBy(s => s.FastestLap);
                    foreach (StandingsItem si in query)
                    {
                        if (si.FastestLap > 0) // skip driver without time
                            si.PositionLive = i++;
                        else
                            si.PositionLive = backmarker--;

                        si.NotifyPosition();
                    }
                    */
                    query = standings.OrderBy(s => s.Position);
                    foreach (StandingsItem si in query)
                    {
                        si.PositionLive = si.Position;
                        si.NotifyPosition();
                    }
                }
            }
        }

        List<SessionInfo> sessions;
        int currentsession;
        int sessionid;
        int subsessionid;
        bool hosted;

        public Sessions()
        {
            sessions = new List<SessionInfo>();
            currentsession = 0;
            sessionid = 0;
            subsessionid = 0;
            hosted = false;
        }

        public List<SessionInfo> SessionList { get { return sessions; } set { sessions = value; } }
        public SessionInfo CurrentSession { get { if (sessions.Count > 0) return sessions[currentsession]; else return new SessionInfo(); } set { } }
        public int SessionId { get { return sessionid; } set { sessionid = value; } }
        public int SubSessionId { get { return subsessionid; } set { subsessionid = value; } }
        public bool Hosted { get { return hosted; } set { hosted = value; } }

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

            this.NotifyPropertyChanged("CurrentSession");
        }

        public SessionInfo findSessionType(SessionInfo.sessionType type)
        {
            int index = sessions.FindIndex(s => s.Type.Equals(type));
            if (index >= 0)
            {
                return SessionList[index];
            }
            else
            {
                return new SessionInfo();
            }
        }
    }

    public class Settings
    {
        public String Theme = "FIA Style";
        public Int32 UpdateFPS = 30;
        public Int32 LapCountdownFrom = 50;
        public Single DeltaDistance = 10;
        public Boolean IncludeMe = false;

        public Int32 OverlayX = 0;
        public Int32 OverlayY = 0;
        public Int32 OverlayW = 1280;
        public Int32 OverlayH = 720;

        public Int32 RemoteControlServerPort = 10700;
        public String RemoteControlServerPassword = "";
        public Boolean RemoteControlServerAutostart = false;

        public Int32 RemoteControlClientPort = 10700;
        public String RemoteControlClientAddress = "";
        public String RemoteControlClientPassword = "";
        public Boolean RemoteControlClientAutostart = false;

        public String WebTimingUrl = "";
        public String WebTimingPassword = "";
        public Int32 WebTimingUpdateInterval = 10;
        public Boolean WebTimingEnable = false;

        public Boolean AlwaysOnTopMainWindow = false;
        public Boolean AlwaysOnTopCameraControls = false;
        public Boolean AlwaysOnTopLists = false;
        public Boolean LoseFocus = false;

        public Boolean CameraControlSortByNumber = false;
        public Boolean CameraControlIncludeSaferyCar = false;

        public Settings(String filename)
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            IniFile ini;

            if (File.Exists(filename))
            {
                ini = new IniFile(filename);

                this.Theme = ini.IniReadValue("theme", "name");
                this.UpdateFPS = Int32.Parse(ini.IniReadValue("theme", "updatefps"));
                this.LapCountdownFrom = Int32.Parse(ini.IniReadValue("theme", "lapcountdownfrom"));

                Single.TryParse(ini.IniReadValue("theme", "deltadistance"), NumberStyles.AllowDecimalPoint, culture, out this.DeltaDistance);
                if (this.DeltaDistance < 0.5)
                    this.DeltaDistance = 10;

                if (ini.IniReadValue("theme", "includeme").ToLower() == "true")
                    this.IncludeMe = true;

                this.OverlayX = Int32.Parse(ini.IniReadValue("overlay", "x"));
                this.OverlayY = Int32.Parse(ini.IniReadValue("overlay", "y"));
                this.OverlayW = Int32.Parse(ini.IniReadValue("overlay", "w"));
                this.OverlayH = Int32.Parse(ini.IniReadValue("overlay", "h"));

                this.RemoteControlServerPassword = ini.IniReadValue("remote control server", "password");
                this.RemoteControlServerPort = Int32.Parse(ini.IniReadValue("remote control server", "port"));
                if (ini.IniReadValue("remote control server", "autostart").ToLower() == "true")
                    this.RemoteControlServerAutostart = true;

                this.RemoteControlClientPassword = ini.IniReadValue("remote control client", "password");
                this.RemoteControlClientPort = Int32.Parse(ini.IniReadValue("remote control client", "port"));
                this.RemoteControlClientAddress = ini.IniReadValue("remote control client", "address");
                if (ini.IniReadValue("remote control client", "autostart").ToLower() == "true")
                    this.RemoteControlClientAutostart = true;

                this.WebTimingPassword = ini.IniReadValue("webtiming", "password");
                this.WebTimingUrl = ini.IniReadValue("webtiming", "url");
                this.WebTimingUpdateInterval = Int32.Parse(ini.IniReadValue("webtiming", "interval"));
                if (ini.IniReadValue("webtiming", "enable").ToLower() == "true")
                    this.WebTimingEnable = true;

                if (ini.IniReadValue("windows", "AlwaysOnTopMainWindow").ToLower() == "true")
                    this.AlwaysOnTopMainWindow = true;
                if (ini.IniReadValue("windows", "AlwaysOnTopCameraControls").ToLower() == "true")
                    this.AlwaysOnTopCameraControls = true;
                if (ini.IniReadValue("windows", "AlwaysOnTopLists").ToLower() == "true")
                    this.AlwaysOnTopLists = true;
                if (ini.IniReadValue("windows", "LoseFocus").ToLower() == "true")
                    this.LoseFocus = true;

                if (ini.IniReadValue("controls", "sortbynumber").ToLower() == "true")
                    this.CameraControlSortByNumber = true;
                if (ini.IniReadValue("controls", "saferycar").ToLower() == "true")
                    this.CameraControlIncludeSaferyCar = true;
            }
            else
            {
                ini = new IniFile(filename);

                ini.IniWriteValue("theme", "name", Properties.Settings.Default.theme);
                ini.IniWriteValue("theme", "updatefps", Properties.Settings.Default.UpdateFrequency.ToString());
                ini.IniWriteValue("theme", "lapcountdownfrom", Properties.Settings.Default.countdownThreshold.ToString());

                ini.IniWriteValue("overlay", "x", Properties.Settings.Default.OverlayLocationX.ToString());
                ini.IniWriteValue("overlay", "y", Properties.Settings.Default.OverlayLocationY.ToString());
                ini.IniWriteValue("overlay", "w", Properties.Settings.Default.OverlayWidth.ToString());
                ini.IniWriteValue("overlay", "h", Properties.Settings.Default.OverlayHeight.ToString());

                ini.IniWriteValue("remote control server", "password", Properties.Settings.Default.remoteServerKey);
                ini.IniWriteValue("remote control server", "port", Properties.Settings.Default.remoteServerPort.ToString());
                ini.IniWriteValue("remote control server", "autostart", Properties.Settings.Default.remoteServerAutostart.ToString().ToLower());

                ini.IniWriteValue("remote control client", "password", Properties.Settings.Default.remoteClientKey);
                ini.IniWriteValue("remote control client", "port", Properties.Settings.Default.remoteClientPort.ToString());
                ini.IniWriteValue("remote control client", "address", Properties.Settings.Default.remoteClientIp);
                ini.IniWriteValue("remote control client", "autostart", Properties.Settings.Default.remoteClientAutostart.ToString().ToLower());

                ini.IniWriteValue("webtiming", "password", Properties.Settings.Default.webTimingKey);
                ini.IniWriteValue("webtiming", "url", Properties.Settings.Default.webTimingUrl);
                ini.IniWriteValue("webtiming", "interval", Properties.Settings.Default.webTimingInterval.ToString());
                ini.IniWriteValue("webtiming", "enable", Properties.Settings.Default.webTimingEnable.ToString().ToLower());

                ini.IniWriteValue("windows", "AlwaysOnTopMainWindow", Properties.Settings.Default.AoTmain.ToString().ToLower());
                ini.IniWriteValue("windows", "AlwaysOnTopCameraControls", Properties.Settings.Default.AoTcontrols.ToString().ToLower());
                ini.IniWriteValue("windows", "AlwaysOnTopLists", Properties.Settings.Default.AoTlists.ToString().ToLower());

                ini.IniWriteValue("controls", "sortbynumber", Properties.Settings.Default.DriverListSortNumber.ToString().ToLower());
                ini.IniWriteValue("controls", "saferycar", Properties.Settings.Default.DriverListIncSC.ToString().ToLower());

            }

            // update ini
            ini.IniWriteValue("theme", "name", this.Theme);
            ini.IniWriteValue("theme", "updatefps", this.UpdateFPS.ToString());
            ini.IniWriteValue("theme", "lapcountdownfrom", this.LapCountdownFrom.ToString());
            ini.IniWriteValue("theme", "deltadistance", this.DeltaDistance.ToString("F5", culture));
            ini.IniWriteValue("theme", "includeme", this.IncludeMe.ToString().ToLower());

            ini.IniWriteValue("overlay", "x", this.OverlayX.ToString());
            ini.IniWriteValue("overlay", "y", this.OverlayY.ToString());
            ini.IniWriteValue("overlay", "w", this.OverlayW.ToString());
            ini.IniWriteValue("overlay", "h", this.OverlayH.ToString());

            ini.IniWriteValue("remote control server", "password", this.RemoteControlServerPassword);
            ini.IniWriteValue("remote control server", "port", this.RemoteControlServerPort.ToString());
            ini.IniWriteValue("remote control server", "autostart", this.RemoteControlServerAutostart.ToString().ToLower());

            ini.IniWriteValue("remote control client", "password", this.RemoteControlClientPassword);
            ini.IniWriteValue("remote control client", "port", this.RemoteControlClientPort.ToString());
            ini.IniWriteValue("remote control client", "address", this.RemoteControlClientAddress);
            ini.IniWriteValue("remote control client", "autostart", this.RemoteControlClientAutostart.ToString().ToLower());

            ini.IniWriteValue("webtiming", "password", this.WebTimingPassword);
            ini.IniWriteValue("webtiming", "url", this.WebTimingUrl);
            ini.IniWriteValue("webtiming", "interval", this.WebTimingUpdateInterval.ToString());
            ini.IniWriteValue("webtiming", "enable  ", this.WebTimingEnable.ToString().ToLower());

            ini.IniWriteValue("windows", "AlwaysOnTopMainWindow", this.AlwaysOnTopMainWindow.ToString().ToLower());
            ini.IniWriteValue("windows", "AlwaysOnTopCameraControls", this.AlwaysOnTopCameraControls.ToString().ToLower());
            ini.IniWriteValue("windows", "AlwaysOnTopLists", this.AlwaysOnTopLists.ToString().ToLower());
            ini.IniWriteValue("windows", "LoseFocus", this.LoseFocus.ToString().ToLower());

            ini.IniWriteValue("controls", "sortbynumber", this.CameraControlSortByNumber.ToString().ToLower());
            ini.IniWriteValue("controls", "saferycar", this.CameraControlIncludeSaferyCar.ToString().ToLower());
        }
    }

    public class TrackInfo
    {
        public Int32 id;
        public Single length;
        public Int32 turns;
        public String name = "";

        public String city = "";
        public String country = "";
        public Single altitude;

        public String sky = "Clear";
        public Single tracktemp;
        public Single airtemp;
        public Int32 humidity;
        public Int32 fog;

        public Single airpressure;
        public Single windspeed;
        public Single winddirection;

    }

    public enum TriggerTypes
    {
        flagGreen,
        flagYellow,
        flagWhite,
        flagCheckered,
        lightsOff,
        lightsReady,
        lightsSet,
        lightsGo,
        replay,
        live
    }

    public enum dataorder
    {
        position,
        liveposition,
        fastestlap,
        previouslap,
        classposition,
        classlaptime,
        points
    }
}
