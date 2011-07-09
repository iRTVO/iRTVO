using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iRSDKSharp;

using System.Threading;
using System.Text.RegularExpressions;
using System.Globalization;

using System.IO;

namespace iRTVO
{
    class iRacingAPI
    {
        public iRacingSDK sdk;
        private int lastUpdate;

        public iRacingAPI()
        {
            sdk = new iRacingSDK();
            lastUpdate = -1;
        }

        private string parseStringValue(string yaml, string key, string suffix = "")
        {
            int length = yaml.Length;
            int start = yaml.IndexOf(key, 0, length);
            if (start >= 0)
            {
                int end = yaml.IndexOf("\n", start, length - start);
                if (end < 0)
                    end = yaml.Length;
                start += key.Length + 2; // skip key name and ": " -separator
                string value = yaml.Substring(start, end - start - suffix.Length);
                return value.Trim();
            }
            else
                return null;
        }

        private int parseIntValue(string yaml, string key, string suffix = "")
        {
            string parsedString = parseStringValue(yaml, key, suffix);
            int value;
            bool result = Int32.TryParse(parsedString, out value);
            if (result)
                return value;
            else
                return Int32.MaxValue;
        }

        private float parseFloatValue(string yaml, string key, string suffix = "")
        {
            NumberStyles style = NumberStyles.AllowDecimalPoint;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

            string parsedString = parseStringValue(yaml, key, suffix);
            double value;
            bool result = Double.TryParse(parsedString, style, culture, out value);
            if (result)
                return (float)value;
            else
                return float.MaxValue;
        }

        private Double parseDoubleValue(string yaml, string key, string suffix = "")
        {
            NumberStyles style = NumberStyles.AllowDecimalPoint;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");

            string parsedString = parseStringValue(yaml, key, suffix);
            double value;
            bool result = Double.TryParse(parsedString, style, culture, out value);
            if (result)
                return value;
            else
                return Double.MaxValue;
        }

        private static Dictionary<String, SessionInfo.sessionType> sessionTypeMap = new Dictionary<String, SessionInfo.sessionType>()
        {
            {"Practice", SessionInfo.sessionType.practice},
            {"Open Qualify", SessionInfo.sessionType.qualify},
            {"Lone Qualify", SessionInfo.sessionType.qualify},
            {"Race", SessionInfo.sessionType.race}
        };

        private void parseFlag(SessionInfo session, Int64 flag)
        {
            /*
            Dictionary<Int32, sessionFlag> flagMap = new Dictionary<Int32, sessionFlag>()
            {
                // global flags
                0x00000001 = sessionFlag.checkered,
                0x00000002 = sessionFlag.white,
                green = sessionFlag.green,
                yellow = 0x00000008,
             * 
                red = 0x00000010,
                blue = 0x00000020,
                debris = 0x00000040,
                crossed = 0x00000080,
             * 
                yellowWaving = 0x00000100,
                oneLapToGreen = 0x00000200,
                greenHeld = 0x00000400,
                tenToGo = 0x00000800,
             * 
                fiveToGo = 0x00001000,
                randomWaving = 0x00002000,
                caution = 0x00004000,
                cautionWaving = 0x00008000,

                // drivers black flags
                black = 0x00010000,
                disqualify = 0x00020000,
                servicible = 0x00040000, // car is allowed service (not a flag)
                furled = 0x00080000,
             * 
                repair = 0x00100000,

                // start lights
                startHidden = 0x10000000,
                startReady = 0x20000000,
                startSet = 0x40000000,
                startGo = 0x80000000,

            };*/

            //Console.WriteLine(flag.ToString("X"));
            /*
            Console.WriteLine("flag: "+ (flag & 0x0000000f).ToString("X"));
            Console.WriteLine("special flag: " + ((flag & 0x000000f0) >> 4 * 1).ToString("X"));
            Console.WriteLine("special flag: " + ((flag & 0x00000f00) >> 4 * 2).ToString("X"));
            Console.WriteLine("special flag: " + ((flag & 0x0000f000) >> 4 * 3).ToString("X"));
            Console.WriteLine("driver flag: "  + ((flag & 0x000f0000) >> 4 * 4).ToString("X"));
            Console.WriteLine("driver flag: "  + ((flag & 0x00f00000) >> 4 * 5).ToString("X"));
            Console.WriteLine("start lights: " + ((flag & 0xf0000000) >> 4 * 7).ToString("X"));
            */
            Int64 regularFlag = flag & 0x0000000f;
            Int64 specialFlag = (flag & 0x0000f000) >> (4 * 3);
            Int64 startlight = (flag & 0xf0000000) >> (4 * 7);

            if (regularFlag == 0x8 || specialFlag >= 0x4)
                session.Flag = SessionInfo.sessionFlag.yellow;
            else if (regularFlag == 0x1)
                session.Flag = SessionInfo.sessionFlag.checkered;
            else if (regularFlag == 0x2)
                session.Flag = SessionInfo.sessionFlag.white;
            else
                session.Flag = SessionInfo.sessionFlag.green;
            

            if (startlight == 0x1)
                session.StartLight = SessionInfo.sessionStartLight.off;
            else if (startlight == 0x2)
                session.StartLight = SessionInfo.sessionStartLight.ready;
            else if (startlight == 0x4)
                session.StartLight = SessionInfo.sessionStartLight.set;
            else if (startlight == 0x8)
                session.StartLight = SessionInfo.sessionStartLight.go;
        }

        private void parser(string yaml) {
            int start = 0;
            int end = 0;
            int length = 0;

            SharedData.mutex = new Mutex(true);

            length = yaml.Length;
            start = yaml.IndexOf("DriverInfo:\n", 0, length);
            end = yaml.IndexOf("\n\n", start, length - start);

            string DriverInfo = yaml.Substring(start, end - start);

            length = DriverInfo.Length;
            start = DriverInfo.IndexOf(" Drivers:\n", 0, length);
            end = length;

            string Drivers = DriverInfo.Substring(start, end - start - 1);
            string[] driverList = Drivers.Split(new string[] { "\n - " }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string driver in driverList)
            {
                int userId = parseIntValue(driver, "UserID");
                if (userId < Int32.MaxValue)
                {
                    int index = SharedData.Drivers.FindIndex(d => d.UserId.Equals(userId));
                    if (index < 0)
                    {
                        DriverInfo driverItem = new DriverInfo();
                        driverItem.Name = parseStringValue(driver, "UserName");
                        driverItem.Shortname = parseStringValue(driver, "AbbrevName");
                        driverItem.Initials = parseStringValue(driver, "Initials");
                        driverItem.Club = parseStringValue(driver, "ClubName");
                        driverItem.License = parseStringValue(driver, "LicLevel"); // TODO
                        driverItem.NumberPlate = parseStringValue(driver, "CarNumber");
                        driverItem.Car = parseStringValue(driver, "CarPath");
                        driverItem.CarId = parseIntValue(driver, "CarID");
                        driverItem.CarClass = parseIntValue(driver, "CarClassID");
                        driverItem.UserId = parseIntValue(driver, "UserID");
                        driverItem.CarIdx = parseIntValue(driver, "CarIdx");
                        SharedData.Drivers.Add(driverItem);
                    }
                }
            }

            length = yaml.Length;
            start = yaml.IndexOf("SessionInfo:\n", 0, length);
            end = yaml.IndexOf("\n\n", start, length - start);

            string SessionInfo = yaml.Substring(start, end - start);

            length = SessionInfo.Length;
            start = SessionInfo.IndexOf(" Sessions:\n", 0, length);
            end = length;

            string Sessions = SessionInfo.Substring(start, end - start);
            string[] sessionList = Sessions.Split(new string[] { "\n - " }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string session in sessionList)
            {
                int sessionNum = parseIntValue(session, "SessionNum");
                if (sessionNum < Int32.MaxValue)
                {
                    int sessionIndex = SharedData.Sessions.SessionList.FindIndex(s => s.Id.Equals(sessionNum));
                    if (sessionIndex < 0)
                    {
                        SessionInfo sessionItem = new SessionInfo();
                        sessionItem.Id = sessionNum; 
                        sessionItem.LapsTotal = parseIntValue(session, "SessionLaps");
                        sessionItem.SessionLength = parseFloatValue(session, "SessionTime", "sec");
                        sessionItem.Type = sessionTypeMap[parseStringValue(session, "SessionType")];
                        sessionItem.Cautions = parseIntValue(session, "ResultsNumCautionFlags");
                        sessionItem.CautionLaps = parseIntValue(session, "ResultsNumCautionLaps");
                        sessionItem.LeadChanges = parseIntValue(session, "ResultsNumLeadChanges");
                        sessionItem.LapsComplete = parseIntValue(session, "ResultsLapsComplete");

                        length = session.Length;
                        start = session.IndexOf("   ResultsFastestLap:\n", 0, length);
                        end = length;
                        string ResultsFastestLap = session.Substring(start, end - start);

                        //sessionItem.FastestLap = parseFloatValue(ResultsFastestLap, "FastestTime");
                    
                        SharedData.Sessions.SessionList.Add(sessionItem);
                        sessionIndex = SharedData.Sessions.SessionList.FindIndex(s => s.Id.Equals(sessionNum));
                    }
                    else // update only non fixed fields
                    {
                        SharedData.Sessions.SessionList[sessionIndex].Cautions = parseIntValue(session, "ResultsNumCautionFlags");
                        SharedData.Sessions.SessionList[sessionIndex].CautionLaps = parseIntValue(session, "ResultsNumCautionLaps");
                        SharedData.Sessions.SessionList[sessionIndex].LeadChanges = parseIntValue(session, "ResultsNumLeadChanges");
                        SharedData.Sessions.SessionList[sessionIndex].LapsComplete = parseIntValue(session, "ResultsLapsComplete");

                        length = session.Length;
                        start = session.IndexOf("   ResultsFastestLap:\n", 0, length);
                        end = length;
                        string ResultsFastestLap = session.Substring(start, end - start);

                        //SharedData.Sessions.SessionList[sessionIndex].FastestLap = parseFloatValue(ResultsFastestLap, "FastestTime");
                    }

                    length = session.Length;
                    start = session.IndexOf("   ResultsPositions:\n", 0, length);
                    end = session.IndexOf("   ResultsFastestLap:\n", start, length - start);

                    string Standings = session.Substring(start, end - start);
                    string[] standingList = Standings.Split(new string[] { "\n   - " }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string standing in standingList)
                    {
                        int carIdx = parseIntValue(standing, "CarIdx");
                        if (carIdx < Int32.MaxValue)
                        {
                            StandingsItem standingItem = new StandingsItem();
                            standingItem = SharedData.Sessions.SessionList[sessionIndex].FindDriver(carIdx);

                            if (parseFloatValue(standing, "LastTime") > 0)
                            {
                                if (parseFloatValue(standing, "LastTime") < SharedData.Sessions.CurrentSession.FastestLap && SharedData.Sessions.CurrentSession.FastestLap > 0)
                                {
                                    Event ev = new Event(
                                        eventType.fastlap,
                                        SharedData.Sessions.CurrentSession.CurrentReplayPosition,
                                        standingItem.Driver,
                                        "New session fastest lap (" + iRTVO.Overlay.floatTime2String(parseFloatValue(standing, "LastTime"), true, false) + ")",
                                        SharedData.Sessions.SessionList[sessionIndex].Type,
                                        parseIntValue(standing, "LapsComplete")
                                    );
                                    SharedData.Events.List.Add(ev);
                                }
                                else if (parseFloatValue(standing, "LastTime") < standingItem.FastestLap && standingItem.FastestLap > 0)
                                {
                                    Event ev = new Event(
                                        eventType.fastlap,
                                        SharedData.Sessions.CurrentSession.CurrentReplayPosition,
                                        standingItem.Driver,
                                        "New PB (" + iRTVO.Overlay.floatTime2String(parseFloatValue(standing, "LastTime"), true, false) + ")",
                                        SharedData.Sessions.SessionList[sessionIndex].Type,
                                        parseIntValue(standing, "LapsComplete")
                                    );
                                    SharedData.Events.List.Add(ev);
                                }
                            }

                            if (parseFloatValue(standing, "FastestTime") < SharedData.Sessions.SessionList[sessionIndex].FastestLap ||
                                SharedData.Sessions.SessionList[sessionIndex].FastestLap <= 0)
                            {
                                SharedData.Sessions.SessionList[sessionIndex].FastestLap = parseFloatValue(standing, "FastestTime");
                            }

                            LapInfo newLap = standingItem.PreviousLap;
                            newLap.LapNum = parseIntValue(standing, "LapsComplete");
                            newLap.LapTime = parseFloatValue(standing, "LastTime");
                            if (newLap.LapTime <= 1)
                            {
                                newLap.LapTime = standingItem.CurrentLap.LapTime;
                            }
                            newLap.Position = parseIntValue(standing, "Position");
                            newLap.Gap = parseFloatValue(standing, "Time");
                            newLap.GapLaps = parseIntValue(standing, "Lap");

                            if(standingItem.Driver.CarIdx < 0)
                            {
                                // insert item
                                int driverIndex = SharedData.Drivers.FindIndex(d => d.CarIdx.Equals(carIdx));
                                standingItem.setDriver(carIdx);
                                standingItem.FastestLap = parseFloatValue(standing, "FastestTime");
                                standingItem.LapsLed = parseIntValue(standing, "LapsLed");

                                standingItem.Laps = new List<LapInfo>();
                                standingItem.Laps.Add(newLap);

                                SharedData.Sessions.SessionList[sessionIndex].Standings.Add(standingItem);

                                standingItem.NotifyLaps();
                                
                            }
                            else
                            {
                                // update item
                                int lapnum =  parseIntValue(standing, "Lap");
                                int lapIndex = standingItem.Laps.FindIndex(l => l.LapNum.Equals(lapnum));

                                // if lapnum not found add it
                                if (lapIndex < 0)
                                {
                                    standingItem.Laps.Add(newLap);
                                    standingItem.NotifyLaps();
                                }
                                standingItem.FastestLap = parseFloatValue(standing, "FastestTime");
                                standingItem.LapsLed = parseIntValue(standing, "LapsLed");
                                standingItem.NotifySelf();
                            }
                        }
                    }
                }
            }

            length = yaml.Length;
            start = yaml.IndexOf("CameraInfo:\n", 0, length);
            end = yaml.IndexOf("\n\n", start, length - start);

            string CameraInfo = yaml.Substring(start, end - start);

            length = CameraInfo.Length;
            start = CameraInfo.IndexOf(" Groups:\n", 0, length);
            end = length;

            string Cameras = CameraInfo.Substring(start, end - start - 1);
            string[] cameraList = Cameras.Split(new string[] { "\n - " }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string camera in cameraList)
            {
                int cameraNum = parseIntValue(camera, "GroupNum");
                if (cameraNum < Int32.MaxValue)
                {
                    CameraGroup camgrp = SharedData.Camera.FindId(cameraNum);
                    //int index = SharedData.Camera.Groups.FindIndex(c => c.Id.Equals(cameraNum));
                    //if (index < 0)
                    if(camgrp.Id < 0)
                    {
                        CameraGroup cameraGroupItem = new CameraGroup();
                        cameraGroupItem.Id = cameraNum;
                        cameraGroupItem.Name = parseStringValue(camera, "GroupName");
                        SharedData.Camera.Groups.Add(cameraGroupItem);
                    }
                }
            }

            length = yaml.Length;
            start = yaml.IndexOf("WeekendInfo:\n", 0, length);
            end = yaml.IndexOf("\n\n", start, length - start);

            string WeekendInfo = yaml.Substring(start, end - start);
            SharedData.Track.length = (Single)parseDoubleValue(WeekendInfo, "TrackLength", "km") * 1000;

            SharedData.Sessions.SessionId = parseIntValue(WeekendInfo, "SessionID");
            SharedData.Sessions.SubSessionId = parseIntValue(WeekendInfo, "SubSessionID");

            length = yaml.Length;
            start = yaml.IndexOf("SplitTimeInfo:\n", 0, length);
            end = yaml.IndexOf("\n\n", start, length - start);

            string SplitTimeInfo = yaml.Substring(start, end - start);

            length = SplitTimeInfo.Length;
            start = SplitTimeInfo.IndexOf(" Sectors:\n", 0, length);
            end = length;

            string Sectors = SplitTimeInfo.Substring(start, end - start - 1);
            string[] sectorList = Sectors.Split(new string[] { "\n - " }, StringSplitOptions.RemoveEmptyEntries);

            if (sectorList.Length != SharedData.Sectors.Count)
            {
                SharedData.Sectors.Clear();
                foreach (string sector in sectorList)
                {
                    int sectornum = parseIntValue(sector, "SectorNum");
                    if (sectornum < 100)
                    {
                        float sectorborder = parseFloatValue(sector, "SectorStartPct");
                        SharedData.Sectors.Add(sectorborder);
                    }
                }
            }

            SharedData.mutex.ReleaseMutex();
        }

        public void getData()
        {
            Single[] DriversTrackPct;
            Int32[] DriversLapNum;
            Int32[] DriversTrackSurface;
            Int32 skip = 0;

            /*
            iRacingSDK sdk = new iRacingSDK();
            int lastUpdate = -1;
            */

            //TextWriter tw = new StreamWriter("output.txt");

            while (true)
            {
                //Check if the SDK is connected
                if (sdk.IsConnected())
                {
                    SharedData.apiConnected = true;
                    SharedData.runOverlay = true;
                    //If it is connected then see if the Session Info has been updated
                    int newUpdate = sdk.Header.SessionInfoUpdate;
                    if (newUpdate != lastUpdate)
                    {
                        lastUpdate = newUpdate;
                        parser(sdk.GetSessionInfo());
                    }

                    SharedData.mutex = new Mutex(true);

                    SharedData.Sessions.setCurrentSession((int)sdk.GetData("SessionNum"));
                    SharedData.Sessions.CurrentSession.Time = (Double)sdk.GetData("SessionTime");
                    SharedData.Sessions.CurrentSession.setFollowedDriver((int)sdk.GetData("CamCarIdx"));
                    SharedData.Sessions.CurrentSession.State = (SessionInfo.sessionState)sdk.GetData("SessionState");
                    parseFlag(SharedData.Sessions.CurrentSession, (Int32)sdk.GetData("SessionFlags"));

                    if (SharedData.Sessions.CurrentSession.SessionStartTime < 0)
                    {
                        SharedData.Sessions.CurrentSession.SessionStartTime = SharedData.Sessions.CurrentSession.Time;
                        SharedData.Sessions.CurrentSession.CurrentReplayPosition = (Int32)sdk.GetData("ReplayFrameNum");
                    }

                    SharedData.Camera.CurrentGroup = (Int32)sdk.GetData("CamGroupNumber");

                    DriversTrackPct = (Single[])sdk.GetData("CarIdxLapDistPct");
                    DriversLapNum = (Int32[])sdk.GetData("CarIdxLap");
                    DriversTrackSurface = (Int32[])sdk.GetData("CarIdxTrackSurface");

                    /*
                    if ((Int32)sdk.GetData("ReplayPlaySpeed") > 0)
                    {
                    */
                        for (Int32 i = 0; i < SharedData.Sessions.CurrentSession.Standings.Count; i++)
                        {
                            StandingsItem driver = SharedData.Sessions.CurrentSession.FindDriver(i);

                            Double prevpos = driver.CurrentTrackPct % 1;
                            Double curpos = DriversTrackPct[i];

                            if (curpos != prevpos && curpos > 0)
                            {
                                Single speed = 0;
                                if ((curpos - prevpos) < 0)
                                {
                                    speed = (Single)((((curpos - prevpos) + 1) * (Double)SharedData.Track.length) * 60);
                                    if ((SurfaceType)DriversTrackSurface[i] != SurfaceType.NotInWorld)
                                    {
                                        Sector sector = new Sector();
                                        sector.Num = driver.Sector;
                                        sector.Time = (Single)(DateTime.Now - driver.SectorBegin).TotalSeconds;
                                        driver.CurrentLap.SectorTimes.Add(sector);
                                        driver.SectorBegin = DateTime.Now;
                                        driver.Sector = 0;


                                        driver.CurrentLap.LapTime = (Single)(DateTime.Now - driver.CurrentLap.Begin).TotalSeconds;
                                        driver.Laps.Add(driver.CurrentLap);

                                        LapInfo newLap = new LapInfo();
                                        newLap.LapNum = DriversLapNum[i];
                                        newLap.Position = driver.Position;
                                        newLap.Begin = DateTime.Now;
                                        driver.CurrentLap = newLap;

                                        
                                    }
                                }
                                else
                                {
                                    speed = (Single)(((curpos - prevpos) * (Double)SharedData.Track.length) * 60);
                                }

                                if (Math.Abs(driver.Prevspeed - speed) < 2) // filter junk
                                {
                                    driver.Speed = speed;
                                }

                                driver.Prevspeed = speed;

                                if (SharedData.SelectedSectors.Count > 0)
                                {
                                    for(int j = 0; j < SharedData.SelectedSectors.Count; j++)
                                    {
                                        if (curpos > SharedData.SelectedSectors[j] && j > driver.Sector)
                                        {
                                            Sector sector = new Sector();
                                            sector.Num = driver.Sector;
                                            sector.Time = (Single)(DateTime.Now - driver.SectorBegin).TotalSeconds;
                                            driver.CurrentLap.SectorTimes.Add(sector);
                                            driver.SectorBegin = DateTime.Now;

                                            driver.Sector = j;
                                        }
                                    }
                                }
                            }

                            driver.CurrentTrackPct = DriversLapNum[i] + DriversTrackPct[i];

                            //if (driver.TrackSurface == SurfaceType.OnTrack && (SurfaceType)DriversTrackSurface[i] == SurfaceType.OffTrack)
                            if (driver.Driver.CarIdx > 0)
                            {
                                if (driver.TrackSurface != (SurfaceType)DriversTrackSurface[i] && 
                                    (SurfaceType)DriversTrackSurface[i] != SurfaceType.OnTrack)
                                {
                                    Event ev = new Event(
                                            eventType.offtrack,
                                            SharedData.Sessions.CurrentSession.CurrentReplayPosition, 
                                            driver.Driver,
                                            ((SurfaceType)DriversTrackSurface[i]).ToString(),
                                            SharedData.Sessions.CurrentSession.Type,
                                            DriversLapNum[i]
                                        );
                                    SharedData.Events.List.Add(ev);
                                }
                                driver.TrackSurface = (SurfaceType)DriversTrackSurface[i];
                            }
                        }

                        if (skip++ >= 60)
                        {
                            SharedData.Sessions.CurrentSession.UpdatePosition();
                            skip = 0;
                        }
                    /*
                    }
                    else
                    {
                        foreach (StandingsItem si in SharedData.Sessions.CurrentSession.Standings)
                        {
                            si.Prevspeed = 0;
                            si.Speed = 0;
                        }
                    }
                    */
                    SharedData.mutex.ReleaseMutex();

                }
                else if (sdk.IsInitialized)
                {
                    sdk.Shutdown();
                    lastUpdate = -1;
                }
                else
                {
                    sdk.Startup();
                }
                System.Threading.Thread.Sleep(15); //tw.Close();
            }
        }
    }
}
