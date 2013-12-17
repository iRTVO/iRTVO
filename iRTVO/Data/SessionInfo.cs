using iRTVO.Interfaces;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class SessionInfo : INotifyPropertyChanged, ISessionInfo
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
                              
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            if (this == SharedData.Sessions.CurrentSession)
                SharedData.NotifyPropertyChanged(name);
        }

        public StandingsItem FindPosition(int pos, DataOrders order)
        {
            return this.FindPosition(pos, order, null);
        }

        public StandingsItem FindPosition(int pos, DataOrders order, string classname)
        {
            int index = -1;
            int i = 1;
            IEnumerable<StandingsItem> query;
            switch (order)
            {
                case DataOrders.fastestlap:
                    Int32 lastpos = SharedData.Drivers.Count;

                    if (classname == null)
                        query = SharedData.Sessions.CurrentSession.Standings.OrderBy(s => s.FastestLap);
                    else
                        query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.Driver.CarClassName == classname).OrderBy(s => s.FastestLap);

                    foreach (StandingsItem si in query)
                    {
                        if (si.FastestLap > 0)
                        {
                            if (i == pos)
                            {
                                index = standings.IndexOf(standings.Where(f => f.Driver.CarIdx.Equals(si.Driver.CarIdx)).FirstOrDefault());
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

                        foreach (StandingsItem si in query)
                        {
                            if (i == pos)
                            {
                                index = standings.IndexOf(standings.Where(f => f.Driver.CarIdx.Equals(si.Driver.CarIdx)).FirstOrDefault());
                                break;
                            }

                            i++;
                        }
                    }
                    break;
                case DataOrders.previouslap:

                    if (classname == null)
                        query = SharedData.Sessions.CurrentSession.Standings.OrderBy(s => s.PreviousLap.LapTime);
                    else
                        query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.Driver.CarClassName == classname).OrderBy(s => s.PreviousLap.LapTime);
                    try
                    {
                        foreach (StandingsItem si in query)
                        {
                            if (si.PreviousLap.LapTime > 0)
                            {
                                if (i == pos)
                                {
                                    index = standings.IndexOf(standings.Where(f => f.Driver.CarIdx.Equals(si.Driver.CarIdx)).FirstOrDefault());
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

                        foreach (StandingsItem si in query)
                        {
                            if (i == pos)
                            {
                                index = standings.IndexOf(standings.Where(f => f.Driver.CarIdx.Equals(si.Driver.CarIdx)).FirstOrDefault());
                                break;
                            }
                            i++;
                        }
                    }
                    break;
                case DataOrders.classposition:
                    query = SharedData.Sessions.CurrentSession.Standings.OrderBy(s => s.Driver.CarClassOrder + s.Position).Skip(pos - 1);
                    if (query.Count() > 0)
                    {
                        StandingsItem si = query.First();
                        return si;
                    }
                    else
                        return new StandingsItem();
                case DataOrders.points:
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
                case DataOrders.liveposition:
                    if (classname == null)
                        index = standings.IndexOf(standings.Where(f => f.PositionLive.Equals(pos)).FirstOrDefault());
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
                case DataOrders.trackposition:
                    if (pos < 0)
                    { // infront
                        int skip = (-pos) - 1;
                        query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.DistanceToFollowed > 0 && s.TrackSurface != SurfaceTypes.NotInWorld).OrderBy(s => s.DistanceToFollowed);
                        if (query.Count() <= skip)
                        {
                            query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.DistanceToFollowed < 0 && s.TrackSurface != SurfaceTypes.NotInWorld).OrderBy(s => s.DistanceToFollowed).Skip((-pos) - 1 - query.Count());
                        }
                        else
                            query = query.Skip(skip);
                    }
                    else if (pos > 0)
                    { // behind
                        int skip = pos - 1;
                        query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.DistanceToFollowed < 0 && s.TrackSurface != SurfaceTypes.NotInWorld).OrderByDescending(s => s.DistanceToFollowed);
                        if (query.Count() <= skip)
                        {
                            query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.DistanceToFollowed > 0 && s.TrackSurface != SurfaceTypes.NotInWorld).OrderByDescending(s => s.DistanceToFollowed).Skip(pos - 1 - query.Count());
                        }
                        else
                            query = query.Skip(skip);
                    }
                    else // me
                        return SharedData.Sessions.CurrentSession.followedDriver;

                    if (query.Count() > 0)
                    {
                        StandingsItem si = query.First();
                        return si;
                    }
                    else
                        return new StandingsItem();
                default:
                    if (classname == null)
                        index = standings.IndexOf(standings.Where(f => f.Position.Equals(pos)).FirstOrDefault());
                    else
                    {
                        query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.Driver.CarClassName == classname).OrderBy(s => s.Position).Skip(pos - 1);
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
            int index = standings.IndexOf(standings.Where(s => s.Driver.CarIdx.Equals(caridx)).FirstOrDefault());
            if (index >= 0)
            {
                return standings[index];
            }
            else
            {
                return new StandingsItem();

            }
        }

        public Int32 getClassPosition(IDriverInfo driver)
        {
            IEnumerable<StandingsItem> query = this.Standings.Where(s => s.Driver.CarClassName == driver.CarClassName).OrderBy(s => s.Position);
            Int32 position = 1;
            foreach (StandingsItem si in query)
            {
                if (si.Driver.CarIdx == driver.CarIdx)
                    return position;
                else
                    position++;
            }
            return 0;
        }

        public Int32 getClassLivePosition(IDriverInfo driver)
        {
            IEnumerable<StandingsItem> query = this.Standings.Where(s => s.Driver.CarClassName == driver.CarClassName).OrderBy(s => s.PositionLive);
            Int32 position = 1;
            foreach (StandingsItem si in query)
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
                IEnumerable<StandingsItem> query = this.Standings.Where(s => s.Driver.CarClassName == className).OrderBy(s => s.Position);
                if (query.Count() > 0)
                {
                    StandingsItem si = query.First();
                    return si;
                }
                else
                    return new StandingsItem();
            }
            else
                return new StandingsItem();
        }

        public Int32 getClassCarCount(string className)
        {
            IEnumerable<StandingsItem> query = this.Standings.Where(s => s.Driver.CarClassName == className);
            return query.Count();
        }

        Int32 id;
        Int32 lapsTotal;
        Int32 lapsComplete;
        Int32 leadChanges;
        Int32 cautions;
        Int32 cautionLaps;

        Single fastestlap = 0;
        DriverInfo fastestdriver;
        Int32 fastestlapnum;

        Double time;
        Double sessiontimeremaining;
        Double sessionlength;
        Double sessionstarttime;
        Int32 sessionstartpos;
        Int32 finishline;

        SessionTypes type;
        SessionStates state;
        SessionFlags flag;
        SessionStartLights startlight;

        StandingsItem followedDriver;
        ObservableCollection<StandingsItem> standings;

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

            type = SessionTypes.none;
            state = SessionStates.invalid;
            flag = SessionFlags.invalid;
            startlight = SessionStartLights.off;

            standings = new ObservableCollection<StandingsItem>();
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

        public Single PreviousFastestLap { get; set; }
        public Single FastestLap { get { return fastestlap; } set { PreviousFastestLap = fastestlap; fastestlap = value; } }
        public DriverInfo FastestLapDriver { get { return fastestdriver; } set { fastestdriver = value; } }
        public Int32 FastestLapNum { get { return fastestlapnum; } set { fastestlapnum = value; } }

        public Double SessionLength { get { return sessionlength; } set { sessionlength = value; } }
        public Double Time { get { return time; } set { time = value; } }
        public Double TimeRemaining { get { return sessiontimeremaining; } set { sessiontimeremaining = value; } }
        public Double SessionStartTime { get { return sessionstarttime; } set { sessionstarttime = value; } }
        public Int32 CurrentReplayPosition { get { return (Int32)((time - sessionstarttime) * 60) + sessionstartpos; } set { sessionstartpos = value; } }
        public Int32 FinishLine { get { return finishline; } set { finishline = value; } }

        public SessionTypes Type { get { return type; } set { type = value; } }
        public SessionStates State { get { return state; } set { state = value; } }
        public SessionFlags Flag { get { return flag; } set { flag = value; } }
        public SessionStartLights StartLight { get { return startlight; } set { startlight = value; } }

        private Boolean _PitOccupied = false;
        public Boolean PitOccupied
        {
            get { return _PitOccupied; }
            set
            {
                if (_PitOccupied == value)
                    return;
                if (_PitOccupied == false) // Someone entered the pit
                {
                    _PitOccupied = value;
                    SharedData.triggers.Push(TriggerTypes.pitOccupied);
                    return;
                }
                // Last ar left the Pits
                _PitOccupied = value;
                SharedData.triggers.Push(TriggerTypes.pitEmpty);
            }
        }

        public void CheckPitStatus()
        {
            if (Type != SessionTypes.race)
            {
                PitOccupied = false;
                return;
            }
            int ct = Standings.Count(s => s.TrackSurface == SurfaceTypes.InPitStall);
            PitOccupied = (ct > 0);
        }

        public ObservableCollection<StandingsItem> Standings { get { return standings; } set { standings = value; } }

        public StandingsItem FollowedDriver { get { return followedDriver; } set { } }

        public void setFollowedDriver(Int32 carIdx)
        {
            if ((followedDriver == null) || (carIdx != followedDriver.Driver.CarIdx))
            {
                logger.Trace("setFollowedDriver Old={0} , new={1}", (followedDriver == null) ? "None" : followedDriver.Driver.CarIdx.ToString(), carIdx);
                followedDriver.IsFollowedDriver = false;
                followedDriver = FindDriver(carIdx);
                if (followedDriver.Driver.CarIdx == carIdx)
                {
                    followedDriver.IsFollowedDriver = true;
                    NotifyPropertyChanged("FollowedDriver");
                }
            }
        }

        public StandingsItem getLeader()
        {
            StandingsItem stand = this.FindPosition(1, DataOrders.position);
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
            StandingsItem stand = this.FindPosition(1, DataOrders.liveposition);
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
            if (this.type == SessionTypes.race)
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

                query = standings.OrderBy(s => s.Position);
                foreach (StandingsItem si in query)
                {
                    si.PositionLive = si.Position;
                    si.NotifyPosition();
                }

            }
            CheckPitStatus();
        }




        IDriverInfo ISessionInfo.FastestLapDriver
        {
            get { return FastestLapDriver as IDriverInfo; }
        }


        IStandingsItem ISessionInfo.FindDriver(int caridx)
        {
            return FindDriver(caridx) as IStandingsItem;
        }

        IStandingsItem ISessionInfo.FindPosition(int pos, DataOrders order)
        {
            return FindPosition(pos, order) as IStandingsItem;
        }

        IStandingsItem ISessionInfo.FindPosition(int pos, DataOrders order, string classname)
        {
            return FindPosition(pos, order, classname) as IStandingsItem;
        }


        IStandingsItem ISessionInfo.FollowedDriver
        {
            get { return FollowedDriver as IStandingsItem; }
        }


        IStandingsItem ISessionInfo.getClassLeader(string className)
        {
            return getClassLeader(className) as IStandingsItem;
        }

       
        IStandingsItem ISessionInfo.getLeader()
        {
            return getLeader() as IStandingsItem;
        }

        IStandingsItem ISessionInfo.getLiveLeader()
        {
            return getLiveLeader() as IStandingsItem;
        }

        IList<IStandingsItem> ISessionInfo.Standings
        {
            get { return Standings.ToArray() as IList<IStandingsItem>; }
        }
        
       
       
    }
}
