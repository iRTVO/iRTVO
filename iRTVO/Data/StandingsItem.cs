using iRTVO.Interfaces;
using iRSDKSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class StandingsItem : INotifyPropertyChanged, IStandingsItem
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            SharedData.NotifyPropertyChanged(name);
        }

        

        DriverInfo driver;
        List<LapInfo> laps;
        Single fastestlap;
        Int32 lapsled;
        Int32 classlapsled;
        SurfaceTypes prevTrackSurface;
        SurfaceTypes currentTrackSurface;
        Double trackpct;
        Double prevtrackpct;
        Double prevtrackpctupdate;
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

        Boolean isFollowedDriver = false;
        Int32 airTimeCount = 0;
        TimeSpan airTimeAirTime = TimeSpan.FromMilliseconds(0.0);
        DateTime airTimeLastAirTime = DateTime.MinValue;

        // KJ: incident-markers and driver-swap timestamp - sadly enough the incident-markers are pretty useless, since other than OffTrack we can't really get incs in real time  :(
        Int32 incidents = 0;
        Int32 lastIncidents = 0;
        Int64 incidentsReplayPos = 0;
        double lastIncidentsTill = 0.0;
        double incidentThreshold = 3.0;

        double lastDriverSwap = 0.0;

        public StandingsItem()
        {
            driver = new DriverInfo();
            laps = new List<LapInfo>();
            fastestlap = 0;
            lapsled = 0;
            classlapsled = 0;
            currentTrackSurface = prevTrackSurface = SurfaceTypes.NotInWorld;
            trackpct = 0;
            prevtrackpct = 0;
            prevtrackpctupdate = 0;
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
        public string FastestLap_HR { get { if (fastestlap != Single.MaxValue) return Utils.floatTime2String(fastestlap, 3, false); else return String.Empty; } }
        public Int32 LapsLed { get { return lapsled; } set { lapsled = value; } }
        public Int32 ClassLapsLed { get { return classlapsled; } set { classlapsled = value; } }
        public SurfaceTypes TrackSurface
        {
            get { return currentTrackSurface; }
            set
            {               
                prevTrackSurface = currentTrackSurface;
                currentTrackSurface = value;

                // Check if Driver went Off-Road
                if (currentTrackSurface == SurfaceTypes.OffTrack && prevTrackSurface != SurfaceTypes.OffTrack)
                {
                    SessionEvent ev = new SessionEvent(
                                            SessionEventTypes.offtrack,
                                            CurrentLap.ReplayPos,
                                            driver,
                                            "Off track",
                                            SharedData.Sessions.CurrentSession.Type,
                                            CurrentLap.LapNum,
                                            SharedData.Sessions.CurrentSession.Id    // KJ: additional data for rewritten "REWIND" broadcast
                                        );
                    SharedData.Events.Add(ev);
                    SharedData.triggers.Push(new TriggerInfo { CarIdx = driver.CarIdx, Trigger = TriggerTypes.offTrack });
                }

                if (prevTrackSurface != currentTrackSurface && currentTrackSurface == SurfaceTypes.NotInWorld)
                {
                    OffTrackSince = SharedData.Sessions.CurrentSession.Time;
                    SharedData.triggers.Push(new TriggerInfo { CarIdx = driver.CarIdx, Trigger = TriggerTypes.notInWorld });
                }

                if (prevTrackSurface != currentTrackSurface && currentTrackSurface == SurfaceTypes.AproachingPits)
                {
                    if (prevTrackSurface == SurfaceTypes.InPitStall)
                    {
                        SharedData.triggers.Push(new TriggerInfo { CarIdx = driver.CarIdx, Trigger = TriggerTypes.pitOut });
                    }
                    else
                    {
                        SharedData.triggers.Push(new TriggerInfo { CarIdx = driver.CarIdx, Trigger = TriggerTypes.pitIn });
                    }
                }

                if (SharedData.Sessions.CurrentSession.Type == SessionTypes.race)
                {
                    // Pit-Stop checks
                    if (currentTrackSurface == SurfaceTypes.InPitStall)
                    {
                        if (prevTrackSurface != SurfaceTypes.InPitStall) // Driver entered the pit 
                        {
                            if ((prevTrackSurface != SurfaceTypes.NotInWorld)) // (not starting from pits!)
                            {
                                if (SharedData.Sessions.CurrentSession.State == SessionStates.racing)
                                {
                                    SessionEvent ev = new SessionEvent(
                                            SessionEventTypes.pit,
                                            CurrentLap.ReplayPos,
                                            Driver,
                                            "Pitting on lap " + CurrentLap.LapNum,
                                            SharedData.Sessions.CurrentSession.Type,
                                            CurrentLap.LapNum,
                                            SharedData.Sessions.CurrentSession.Id     // KJ: additional data for rewritten "REWIND" Broadcast
                                        );
                                    SharedData.Events.Add(ev);
                                    PitStops++;
                                }
                                PitStopBegin = DateTime.Now;
                                NotifyPit();
                            }
                        }
                        else
                        {
                            PitStopTime = (Single)(DateTime.Now - PitStopBegin).TotalSeconds;
                            NotifyPit();
                        }
                    }
                }

                NotifyPropertyChanged("TrackSurface");
            }
        }
        public SurfaceTypes PrevTrackSurface { get { return prevTrackSurface; } }
        public Int32 Sector { get { return sector; } set { sector = value; } }
        public Double SectorBegin { get { return sectorbegin; } set { sectorbegin = value; } }
        public Int32 PitStops { get { return pitstops; } set { pitstops = value; } }
        public Single PitStopTime { get { return pitstoptime; } set { pitstoptime = value; } }
        public DateTime PitStopBegin { get { return pitstorbegin; } set { pitstorbegin = value; } }
        public Double Begin { get { return begin; } set { begin = value; } }
        public Boolean Finished { get { return finished; } set { finished = value; } }
        public Double OffTrackSince { get { return offtracksince; } set { offtracksince = value; } }
        public Double PrevTrackPct { get { return prevtrackpct; } set { prevtrackpct = value; } }
        public Double PrevTrackPctUpdate { get { return prevtrackpctupdate; } set { prevtrackpctupdate = value; } }

        public Int32 AirTimeCount { get { return airTimeCount; } }
        public TimeSpan AirTimeAirTime { get { return airTimeAirTime; } set { airTimeAirTime = value; NotifyPropertyChanged("AirTimeAirTime"); NotifyPropertyChanged("AirTimeAirTime_HR"); } }
        public String AirTimeAirTime_HR { get { return String.Format("{0:hh\\:mm\\:ss}", airTimeAirTime); } }
        public DateTime AirTimeLastAirTime { get { return airTimeLastAirTime; } }

        // KJ: incident-markers and driver-swap timestamp - sadly enough the incident-markers are pretty useless, since other than OffTrack we can't really get incs in real time  :(
        public Int32 Incidents { get { return incidents; } set { incidents = value; } }
        public Int32 LastIncidents { get { return lastIncidents; } set { lastIncidents = value; } }
        public double LastIncidentsTill { get { return lastIncidentsTill; } set { lastIncidentsTill = value; } }
        public double IncidentThreshold { get { return incidentThreshold; } set { incidentThreshold = value; } }
        public Int64 IncidentsReplayPos { get { return incidentsReplayPos; } set { incidentsReplayPos = value; } }

        public double LastDriverSwap { get { return lastDriverSwap; } set { lastDriverSwap = value; } }

        public void AddAirTime(Double howmuch)
        {
            if (howmuch > 0.0)
                AirTimeAirTime = airTimeAirTime.Add(TimeSpan.FromSeconds(howmuch));
        }

        public bool IsFollowedDriver
        {
            get { return isFollowedDriver; }
            set
            {
                airTimeLastAirTime = DateTime.Now;
                if (!isFollowedDriver && (value == true))
                {
                    airTimeCount++;
                    NotifyPropertyChanged("AirTimeCount");
                }
                isFollowedDriver = value;
                NotifyPropertyChanged("IsFollowedDriver");
                NotifyPropertyChanged("AirTimeLastAirTime");
            }
        }

        public LapInfo CurrentLap
        {
            get
            {
                if (currentTrackSurface == SurfaceTypes.NotInWorld && finished == false)
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

        public Double TrackPct
        {
            get
            {
                return this.trackpct % 1;
            }
            set { }
        }

        public Double DistanceToFollowed
        {
            get
            {
                //Console.WriteLine("P" + this.position + " to P" + SharedData.Sessions.CurrentSession.FollowedDriver.Position + " = " + ((this.trackpct - SharedData.Sessions.CurrentSession.FollowedDriver.CurrentTrackPct) % 1.0));
                return (this.trackpct % 1) - SharedData.Sessions.CurrentSession.FollowedDriver.TrackPct;
            }
            set { }
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
                    return SharedData.timedelta.GetDelta(this.driver.CarIdx, SharedData.Sessions.CurrentSession.FindPosition(this.positionlive - 1, DataOrders.liveposition).driver.CarIdx).TotalSeconds;
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
            else if ((SharedData.Sessions.CurrentSession.FindPosition(this.positionlive - 1, DataOrders.liveposition).CurrentTrackPct - trackpct) > 1)
            {
                return (SharedData.Sessions.CurrentSession.FindPosition(this.positionlive - 1, DataOrders.liveposition).CurrentLap.LapNum - currentlap.LapNum) + "L";
            }
            else
            {
                return Utils.floatTime2String((float)IntervalLive, rounding, false);//  Theme.round(IntervalLive, rounding);
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
                return Utils.floatTime2String((float)GapLive, rounding, false);//                    Theme.round(GapLive, rounding);
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
                else if ((SharedData.Sessions.CurrentSession.FindPosition(this.positionlive - 1, DataOrders.liveposition, this.driver.CarClassName).CurrentTrackPct - trackpct) > 1)
                {
                    return (SharedData.Sessions.CurrentSession.FindPosition(this.positionlive - 1, DataOrders.liveposition, this.driver.CarClassName).CurrentLap.LapNum - currentlap.LapNum) + "L";
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

        public Double IntervalToFollowedLive
        {
            get
            {
                if (this.driver.CarIdx == SharedData.Sessions.CurrentSession.FollowedDriver.Driver.CarIdx)
                    return 0.0;
                if (this.positionlive > SharedData.Sessions.CurrentSession.FollowedDriver.PositionLive)
                    return SharedData.timedelta.GetDelta(this.driver.CarIdx, SharedData.Sessions.CurrentSession.FollowedDriver.Driver.CarIdx).TotalSeconds;
                else
                    return SharedData.timedelta.GetDelta(SharedData.Sessions.CurrentSession.FollowedDriver.Driver.CarIdx, this.driver.CarIdx).TotalSeconds;
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

        

        ILapInfo IStandingsItem.CurrentLap
        {
            get { return CurrentLap as ILapInfo; }
        }

        ILapInfo IStandingsItem.PreviousLap { get { return PreviousLap as ILapInfo; } }

        IDriverInfo IStandingsItem.Driver
        {
            get { return Driver as IDriverInfo; }
        }
        
        ILapInfo IStandingsItem.FindLap(int num)
        {
            return FindLap(num) as ILapInfo;
        }        

        IList<ILapInfo> IStandingsItem.Laps
        {
            get { return Laps as IList<ILapInfo>; }
        }

        
    }

}
