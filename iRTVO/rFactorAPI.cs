using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.MemoryMappedFiles;
using Newtonsoft.Json;

namespace iRTVO
{
    class rFactorAPI
    {
        // MMF access
        MemoryMappedFile mmap;
        MemoryMappedViewAccessor shmem;

        // status
        Boolean initialized;

        // buffer
        const int bufferSize = 1024000;
        const int maxDrivers = 64;

        public void Startup()
        {
            try
            {
                mmap = System.IO.MemoryMappedFiles.MemoryMappedFile.OpenExisting(
                   "iRTVOrFactor",
                   MemoryMappedFileRights.Read);

                shmem = mmap.CreateViewAccessor(0, bufferSize, MemoryMappedFileAccess.Read);

                initialized = true;
            }
            catch
            {
                initialized = false;
            }
        }

        public void getData()
        {
            int PreviousTimestamp = 0;
            double PreviousSessionTime = 0.0;
            double[] DriversTrackPct = new double[maxDrivers];

            while (true)
            {
                if (initialized && SharedData.runApi)
                {
                    Int32 timestamp = shmem.ReadInt32(0);
                    Int32 jsonSize = shmem.ReadInt32(4);

                    if (timestamp > PreviousTimestamp)
                    {
                        Byte[] buf = new Byte[jsonSize];
                        shmem.ReadArray<Byte>(8, buf, 0, jsonSize);
                        char[] charsToTrim = { '\0', '\n', '\r', '\t', '"' };
                        String json = Encoding.UTF8.GetString(buf, 0, jsonSize).Trim(charsToTrim);
                        rFactorDataFormat data = Newtonsoft.Json.JsonConvert.DeserializeObject<rFactorDataFormat>(json);

                        if (data.Session.SessionTime > PreviousSessionTime)
                        {

                            // wait and lock
                            SharedData.mutex.WaitOne();

                            SharedData.Track.length = (float)data.Session.TrackLength;

                            // drivers
                            foreach (rFactorDataFormat.DriverDataFormat driver in data.Drivers)
                            {
                                int index = SharedData.Drivers.FindIndex(item => item.CarIdx == driver.id);
                                if (index < 0)
                                {
                                    DriverInfo newDriver = new DriverInfo();
                                    newDriver.CarIdx = driver.id;
                                    newDriver.Name = driver.Name;
                                    SharedData.Drivers.Add(newDriver);
                                }
                            }

                            // sessions
                            Sessions.SessionInfo.sessionType sessiontype;

                            switch (data.Session.SessionType)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                    sessiontype = Sessions.SessionInfo.sessionType.practice;
                                    break;
                                case 5:
                                case 6:
                                case 7:
                                case 8:
                                    sessiontype = Sessions.SessionInfo.sessionType.qualify;
                                    break;
                                case 9:
                                    sessiontype = Sessions.SessionInfo.sessionType.warmup;
                                    break;
                                case 10:
                                case 11:
                                case 12:
                                case 13:
                                    sessiontype = Sessions.SessionInfo.sessionType.race;
                                    break;
                                default:
                                    sessiontype = Sessions.SessionInfo.sessionType.invalid;
                                    break;
                            }

                            Sessions.SessionInfo session = SharedData.Sessions.findSessionType(sessiontype);

                            // if not found add to list
                            if (session.Type != sessiontype)
                                SharedData.Sessions.SessionList.Add(session);

                            session.Type = sessiontype;
                            session.SessionLength = data.Session.SessionLength;
                            session.Time = data.Session.SessionTime;
                            session.TimeRemaining = session.SessionLength - session.Time;

                            switch (data.Session.SessionState)
                            {
                                case 1: // Reconnaissance laps (race only)
                                    session.State = Sessions.SessionInfo.sessionState.warmup;
                                    session.Flag = Sessions.SessionInfo.sessionFlag.yellow;
                                    break;
                                case 2: // Grid walk-through (race only)
                                    session.State = Sessions.SessionInfo.sessionState.gridding;
                                    session.Flag = Sessions.SessionInfo.sessionFlag.yellow;
                                    break;
                                case 3: // Formation lap (race only)
                                    session.State = Sessions.SessionInfo.sessionState.pacing;
                                    session.Flag = Sessions.SessionInfo.sessionFlag.yellow;
                                    break;
                                case 4: // Starting-light countdown has begun (race only)
                                    session.State = Sessions.SessionInfo.sessionState.pacing;
                                    break;
                                case 5: // Green flag
                                    session.State = Sessions.SessionInfo.sessionState.racing;
                                    session.Flag = Sessions.SessionInfo.sessionFlag.green;
                                    break;
                                case 6: // Full course yellow / safety car
                                    session.State = Sessions.SessionInfo.sessionState.racing;
                                    session.Flag = Sessions.SessionInfo.sessionFlag.yellow;
                                    break;
                                case 7: // Session stopped
                                    session.State = Sessions.SessionInfo.sessionState.racing;
                                    session.Flag = Sessions.SessionInfo.sessionFlag.red;
                                    break;
                                case 8: // Session over
                                    session.State = Sessions.SessionInfo.sessionState.checkered;
                                    session.Flag = Sessions.SessionInfo.sessionFlag.checkered;
                                    break;
                                case 0: // Before session has begun
                                default:
                                    session.State = Sessions.SessionInfo.sessionState.invalid;
                                    session.Flag = Sessions.SessionInfo.sessionFlag.invalid;
                                    break;
                            }

                            foreach (rFactorDataFormat.DriverDataFormat driver in data.Drivers)
                            {
                                Sessions.SessionInfo.StandingsItem si = session.FindDriver(driver.id);
                                si.TrackSurface = Sessions.SessionInfo.StandingsItem.SurfaceType.OnTrack;
                                si.CurrentTrackPct = driver.LapsCompleted + driver.TrackPct;
                                // if not found add to session
                                if (si.Driver.CarIdx != driver.id)
                                {
                                    si.setDriver(driver.id);
                                    session.Standings.Add(si);

                                    // initialize

                                    // previous
                                    LapInfo previous = new LapInfo();
                                    previous.LapNum = driver.LapsCompleted;
                                    previous.LapTime = (float)driver.PreviousLap;
                                    si.Laps.Add(previous);

                                    // current
                                    LapInfo current = new LapInfo();
                                    current.LapNum = driver.LapsCompleted + 1;
                                    si.Laps.Add(current);
                                    si.CurrentLap = current;

                                    // fastest
                                    si.FastestLap = (float)driver.FastestLap;

                                }

                                // new lap
                                //if (driver.LapsCompleted >= si.CurrentLap.LapNum)
                                if (driver.TrackPct < 0.1 && si.PrevTrackPct > 0.9) // crossing s/f line
                                {
                                    if (driver.Position == 1)
                                        si.LapsLed += 1;

                                    LapInfo currentlap = new LapInfo();                                    
                                    si.Laps.Add(currentlap);

                                    // prev lap time
                                    si.PreviousLap.LapTime = (float)driver.PreviousLap;

                                    // new current lap
                                    si.CurrentLap = currentlap;
                                    si.CurrentLap.LapNum = driver.LapsCompleted + 1;

                                    si.FastestLap = (float)driver.FastestLap;
                                    si.NotifyLaps();
                                }
                                
                                si.CurrentLap.Position = driver.Position;
                                si.CurrentLap.SessionTime = data.Session.SessionTime;

                                si.Position = driver.Position;
                                // calculate speed
                                Double prevpos = si.PrevTrackPct;
                                Double curpos = driver.TrackPct;
                                Single speed;
                                if (curpos < 0.1 && prevpos > 0.9) // crossing s/f line
                                {
                                    speed = (Single)((((curpos - prevpos) + 1) * (Double)SharedData.Track.length) / (data.Session.SessionTime - PreviousSessionTime));
                                }
                                else
                                {
                                    speed = (Single)(((curpos - prevpos) * (Double)SharedData.Track.length) / (data.Session.SessionTime - PreviousSessionTime));
                                }
                                si.Speed = speed;
                                si.PrevTrackPct = driver.TrackPct;

                                si.NotifyPosition();

                                si.PitStops = driver.PitStops;
                                if (driver.InPits)
                                    si.NotifyPit();

                                DriversTrackPct[driver.id] = si.CurrentTrackPct;
                            }

                            SharedData.timedelta.Update(data.Session.SessionTime, DriversTrackPct);
                            SharedData.Sessions.CurrentSession.UpdatePosition();

                            SharedData.apiConnected = true;
                            SharedData.runOverlay = true;

                            SharedData.mutex.ReleaseMutex();
                            PreviousSessionTime = data.Session.SessionTime;
                        }
                        PreviousTimestamp = timestamp;
                    }
                    else
                        System.Threading.Thread.Sleep(4);
                }
                else if (SharedData.runApi == true)
                {
                    Startup();
                }

                if (SharedData.runApi == false)
                    break;
            }
        }
    }

    class rFactorDataFormat
    {
        public class SessionDataFormat
        {
            public string TrackName { get; set; }
            public double TrackLength { get; set; }
            public int SessionType { get; set; }
            public int SessionState { get; set; }
            public double SessionTime { get; set; }
            public double SessionLength { get; set; }
            public int SessionLaps { get; set; }
            public int Flag { get; set; }
            public int Temperature { get; set; }
            public int TrackTemperature { get; set; }
        }

        public class DriverDataFormat
        {
            public int id { get; set; }
            public string Name { get; set; }
            public int LapsCompleted { get; set; }
            public double TrackPct { get; set; }
            public double PreviousLap { get; set; }
            public double FastestLap { get; set; }
            public double CurrentLapBegin { get; set; }
            public int PitStops { get; set; }
            public bool InPits { get; set; }
            public bool StoppedInPits { get; set; }
            public int Position { get; set; }
        }

        public SessionDataFormat Session;
        public IList<DriverDataFormat> Drivers;
    }
}
