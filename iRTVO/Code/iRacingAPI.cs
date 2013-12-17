using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iRSDKSharp;

using System.Threading;
using System.Text.RegularExpressions;
using System.Globalization;

using System.IO;

using NLog;
using iRTVO.Networking;
using iRTVO.Data;
using iRTVO.Interfaces;

namespace iRTVO
{
    class iRacingAPI : iRTVO.Interfaces.ISimulationAPI
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

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
                if (start < end)
                    return yaml.Substring(start, end - start - suffix.Length).Trim();
                else
                    return null;
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
            string parsedString = parseStringValue(yaml, key, suffix);
            double value;
            bool result = Double.TryParse(parsedString, NumberStyles.AllowDecimalPoint, CultureInfo.CreateSpecificCulture("en-US"), out value);
            if (result)
                return (float)value;
            else
                return float.MaxValue;
        }

        private Double parseDoubleValue(string yaml, string key, string suffix = "")
        {
            string parsedString = parseStringValue(yaml, key, suffix);
            double value;
            bool result = Double.TryParse(parsedString, NumberStyles.AllowDecimalPoint, CultureInfo.CreateSpecificCulture("en-US"), out value);
            if (result)
                return value;
            else
                return Double.MaxValue;
        }

        private static Dictionary<String, SessionTypes> sessionTypeMap = new Dictionary<String, SessionTypes>()
        {
            {"Offline Testing", SessionTypes.practice},
            {"Practice", SessionTypes.practice},
            {"Open Qualify", SessionTypes.qualify},
            {"Lone Qualify", SessionTypes.qualify},
            {"Race", SessionTypes.race}
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

            Int64 regularFlag = flag & 0x0000000f;
            Int64 specialFlag = (flag & 0x0000f000) >> (4 * 3);
            Int64 startlight = (flag & 0xf0000000) >> (4 * 7);

            if (regularFlag == 0x8 || specialFlag >= 0x4)
                session.Flag = SessionFlags.yellow;
            else if (regularFlag == 0x2)
                session.Flag = SessionFlags.white;
            else if (regularFlag == 0x1)
                session.Flag = SessionFlags.checkered;
            else
                session.Flag = SessionFlags.green;

            session.StartLight = (SessionStartLights)startlight;
        }

        private void parser(string yaml)
        {
            int start = 0;
            int end = 0;
            int length = 0;

            length = yaml.Length;
            start = yaml.IndexOf("WeekendInfo:\n", 0, length);
            end = yaml.IndexOf("\n\n", start, length - start);

            string WeekendInfo = yaml.Substring(start, end - start);
            SharedData.Track.Length = (Single)parseDoubleValue(WeekendInfo, "TrackLength", "km") * 1000;
            SharedData.Track.Id = parseIntValue(WeekendInfo, "TrackID");
            SharedData.Track.Turns = parseIntValue(WeekendInfo, "TrackNumTurns");
            SharedData.Track.City = parseStringValue(WeekendInfo, "TrackCity");
            SharedData.Track.Country = parseStringValue(WeekendInfo, "TrackCountry");

            SharedData.Track.Altitude = (Single)parseDoubleValue(WeekendInfo, "TrackAltitude", "m");
            SharedData.Track.Sky = parseStringValue(WeekendInfo, "TrackSkies");
            SharedData.Track.TrackTemperature = (Single)parseDoubleValue(WeekendInfo, "TrackSurfaceTemp", "C");
            SharedData.Track.AirTemperature = (Single)parseDoubleValue(WeekendInfo, "TrackAirTemp", "C");
            SharedData.Track.AirPressure = (Single)parseDoubleValue(WeekendInfo, "TrackAirPressure", "Hg");
            SharedData.Track.WindSpeed = (Single)parseDoubleValue(WeekendInfo, "TrackWindVel", "m/s");
            SharedData.Track.WindDirection = (Single)parseDoubleValue(WeekendInfo, "TrackWindDir", "rad");
            SharedData.Track.Humidity = parseIntValue(WeekendInfo, "TrackRelativeHumidity", "%");
            SharedData.Track.Fog = parseIntValue(WeekendInfo, "TrackFogLevel", "%");

            if (parseIntValue(WeekendInfo, "Official") == 0 &&
                parseIntValue(WeekendInfo, "SeasonID") == 0 &&
                parseIntValue(WeekendInfo, "SeriesID") == 0)
                SharedData.Sessions.Hosted = true;
            else
                SharedData.Sessions.Hosted = false;




            if ( SharedData.theme != null )
                SharedData.Track.Name = SharedData.theme.TrackNames.getValue("Tracks", SharedData.Track.Id.ToString(),false,"Unknown Track",false);

            SharedData.Sessions.SessionId = parseIntValue(WeekendInfo, "SessionID");
            SharedData.Sessions.SubSessionId = parseIntValue(WeekendInfo, "SubSessionID");

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
                if (userId < Int32.MaxValue && userId > 0)
                {
                    int index = SharedData.Drivers.FindIndex(d => d.UserId.Equals(userId));
                    if (index < 0 &&
                        parseStringValue(driver, "CarPath") != "safety pcfr500s" &&
                        parseStringValue(driver, "AbbrevName") != "Pace Car")
                    {
                        if (SharedData.settings.IncludeMe || (!SharedData.settings.IncludeMe && parseIntValue(driver, "CarIdx") != 63))
                        {
                            DriverInfo driverItem = new DriverInfo();

                            driverItem.Name = parseStringValue(driver, "UserName");

                            if (parseStringValue(driver, "AbbrevName") != null)
                            {
                                string[] splitName = parseStringValue(driver, "AbbrevName").Split(',');
                                if (splitName.Length > 1)
                                    driverItem.Shortname = splitName[1] + " " + splitName[0];
                                else
                                    driverItem.Shortname = parseStringValue(driver, "AbbrevName");
                            }
                            driverItem.Initials = parseStringValue(driver, "Initials");
                            driverItem.Club = parseStringValue(driver, "ClubName");
                            driverItem.NumberPlate = parseStringValue(driver, "CarNumber");
                            driverItem.CarId = parseIntValue(driver, "CarID");
                            driverItem.CarClass = parseIntValue(driver, "CarClassID");
                            driverItem.UserId = parseIntValue(driver, "UserID");
                            driverItem.CarIdx = parseIntValue(driver, "CarIdx");
                            driverItem.CarClassName = ( SharedData.theme != null ? SharedData.theme.getCarClass(driverItem.CarId) : "unknown" );
                            driverItem.iRating = parseIntValue(driver, "IRating");

                            int liclevel = parseIntValue(driver, "LicLevel");
                            int licsublevel = parseIntValue(driver, "LicSubLevel");

                            switch (liclevel)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                    driverItem.SR = "R" + ((double)licsublevel / 100).ToString("0.00");
                                    break;
                                case 5:
                                case 6:
                                case 7:
                                case 8:
                                    driverItem.SR = "D" + ((double)licsublevel / 100).ToString("0.00");
                                    break;
                                case 9:
                                case 10:
                                case 11:
                                case 12:
                                    driverItem.SR = "C" + ((double)licsublevel / 100).ToString("0.00");
                                    break;
                                case 14:
                                case 15:
                                case 16:
                                case 17:
                                    driverItem.SR = "B" + ((double)licsublevel / 100).ToString("0.00");
                                    break;
                                case 18:
                                case 19:
                                case 20:
                                case 21:
                                    driverItem.SR = "A" + ((double)licsublevel / 100).ToString("0.00");
                                    break;
                                case 22:
                                case 23:
                                case 24:
                                case 25:
                                    driverItem.SR = "P" + ((double)licsublevel / 100).ToString("0.00");
                                    break;
                                case 26:
                                case 27:
                                case 28:
                                case 29:
                                    driverItem.SR = "WC" + ((double)licsublevel / 100).ToString("0.00");
                                    break;
                                default:
                                    driverItem.SR = "Unknown";
                                    break;
                            }

                            driverItem.CarClass = -1;
                            int carclass = parseIntValue(driver, "CarClassID");
                            int freeslot = -1;

                            for (int i = 0; i < SharedData.Classes.Length; i++)
                            {
                                if (SharedData.Classes[i] == carclass)
                                {
                                    driverItem.CarClass = i;
                                }
                                else if (SharedData.Classes[i] == -1 && freeslot < 0)
                                {
                                    freeslot = i;
                                }
                            }

                            if (driverItem.CarClass < 0 && freeslot >= 0)
                            {
                                SharedData.Classes[freeslot] = carclass;
                                driverItem.CarClass = freeslot;
                            }

                            if (!SharedData.externalPoints.ContainsKey(userId) && driverItem.CarIdx < 60)
                                SharedData.externalPoints.Add(userId, 0);

                            // fix bugges
                            if (driverItem.NumberPlate == null)
                                driverItem.NumberPlate = "000";
                            if (driverItem.Initials == null)
                                driverItem.Initials = "";

                            SharedData.Drivers.Add(driverItem);
                        }
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

            // Get Current running Session
            int _CurrentSession = (int)sdk.GetData("SessionNum");

            foreach (string session in sessionList)
            {
                int sessionNum = parseIntValue(session, "SessionNum");
                if (sessionNum < Int32.MaxValue)
                {
                    int sessionIndex = SharedData.Sessions.SessionList.FindIndex(s => s.Id.Equals(sessionNum));
                    if (sessionIndex < 0) // add new session item
                    {
                        SessionInfo sessionItem = new SessionInfo();
                        sessionItem.Id = sessionNum;
                        sessionItem.LapsTotal = parseIntValue(session, "SessionLaps");
                        sessionItem.SessionLength = parseFloatValue(session, "SessionTime", "sec");
                        sessionItem.Type = sessionTypeMap[parseStringValue(session, "SessionType")];

                        if (sessionItem.Type == SessionTypes.race)
                        {
                            sessionItem.FinishLine = parseIntValue(session, "SessionLaps") + 1;
                        }
                        else
                        {
                            sessionItem.FinishLine = Int32.MaxValue;
                        }

                        if (sessionItem.FinishLine < 0)
                        {
                            sessionItem.FinishLine = Int32.MaxValue;
                        }

                        sessionItem.Cautions = parseIntValue(session, "ResultsNumCautionFlags");
                        sessionItem.CautionLaps = parseIntValue(session, "ResultsNumCautionLaps");
                        sessionItem.LeadChanges = parseIntValue(session, "ResultsNumLeadChanges");
                        sessionItem.LapsComplete = parseIntValue(session, "ResultsLapsComplete");

                        length = session.Length;
                        start = session.IndexOf("   ResultsFastestLap:\n", 0, length);
                        end = length;
                        string ResultsFastestLap = session.Substring(start, end - start);

                        sessionItem.FastestLap = parseFloatValue(ResultsFastestLap, "FastestTime");
                        int index = SharedData.Drivers.FindIndex(d => d.CarIdx.Equals(parseIntValue(ResultsFastestLap, "CarIdx")));
                        if (index >= 0)
                        {
                            sessionItem.FastestLapDriver = SharedData.Drivers[index];
                            sessionItem.FastestLapNum = parseIntValue(ResultsFastestLap, "FastestLap");
                        }
                        SharedData.Sessions.SessionList.Add(sessionItem);
                        sessionIndex = SharedData.Sessions.SessionList.FindIndex(s => s.Id.Equals(sessionNum));
                    }
                    else // update only non fixed fields
                    {
                        SharedData.Sessions.SessionList[sessionIndex].LeadChanges = parseIntValue(session, "ResultsNumLeadChanges");
                        SharedData.Sessions.SessionList[sessionIndex].LapsComplete = parseIntValue(session, "ResultsLapsComplete");

                        length = session.Length;
                        start = session.IndexOf("   ResultsFastestLap:\n", 0, length) + "   ResultsFastestLap:\n".Length;
                        end = length;
                        string ResultsFastestLap = session.Substring(start, end - start);

                        SharedData.Sessions.SessionList[sessionIndex].FastestLap = parseFloatValue(ResultsFastestLap, "FastestTime");
                        int index = SharedData.Drivers.FindIndex(d => d.CarIdx.Equals(parseIntValue(ResultsFastestLap, "CarIdx")));
                        if (index >= 0)
                        {
                            SharedData.Sessions.SessionList[sessionIndex].FastestLapDriver = SharedData.Drivers[index];
                            SharedData.Sessions.SessionList[sessionIndex].FastestLapNum = parseIntValue(ResultsFastestLap, "FastestLap");
                        }
                    }

                    // Trigger Overlay Event, but only in current active session
                    if ((SharedData.Sessions.SessionList[sessionIndex].FastestLap != SharedData.Sessions.SessionList[sessionIndex].PreviousFastestLap)
                        && (_CurrentSession == SharedData.Sessions.SessionList[sessionIndex].Id)
                        )
                    {
                        if (SharedData.Sessions.SessionList[sessionIndex].FastestLap > 0)
                        {
                            // Push Event to Overlay
                            logger.Info("New fastet lap in Session {0} : {1}", _CurrentSession, SharedData.Sessions.SessionList[sessionIndex].FastestLap);
                            SharedData.triggers.Push(TriggerTypes.fastestlap);
                        }
                    }


                    length = session.Length;
                    start = session.IndexOf("   ResultsPositions:\n", 0, length);
                    end = session.IndexOf("   ResultsFastestLap:\n", start, length - start);

                    string Standings = session.Substring(start, end - start);
                    string[] standingList = Standings.Split(new string[] { "\n   - " }, StringSplitOptions.RemoveEmptyEntries);

                    Int32 position = 1;
                    List<DriverInfo> standingsDrivers = SharedData.Drivers.ToList();

                    foreach (string standing in standingList)
                    {
                        int carIdx = parseIntValue(standing, "CarIdx");
                        if (carIdx < Int32.MaxValue)
                        {
                            StandingsItem standingItem = new StandingsItem();
                            standingItem = SharedData.Sessions.SessionList[sessionIndex].FindDriver(carIdx);

                            standingsDrivers.Remove(standingsDrivers.Find(s => s.CarIdx.Equals(carIdx)));

                            if (parseFloatValue(standing, "LastTime") > 0)
                            {
                                if (parseFloatValue(standing, "LastTime") < SharedData.Sessions.SessionList[sessionIndex].FastestLap && SharedData.Sessions.SessionList[sessionIndex].FastestLap > 0)
                                {
                                    SessionEvent ev = new SessionEvent(
                                        SessionEventTypes.fastlap,
                                        (Int32)(((Double)sdk.GetData("SessionTime") * 60) + timeoffset),
                                        standingItem.Driver,
                                        "New session fastest lap (" + Utils.floatTime2String(parseFloatValue(standing, "LastTime"), 3, false) + ")",
                                        SharedData.Sessions.SessionList[sessionIndex].Type,
                                        parseIntValue(standing, "LapsComplete")
                                    );
                                    SharedData.Events.Add(ev);
                                }
                            }

                            if (parseFloatValue(standing, "FastestTime") < SharedData.Sessions.SessionList[sessionIndex].FastestLap ||
                                SharedData.Sessions.SessionList[sessionIndex].FastestLap <= 0)
                            {
                                SharedData.Sessions.SessionList[sessionIndex].FastestLap = parseFloatValue(standing, "FastestTime");
                            }

                            /*
                            if (standingItem.Finished == false)
                            {
                                standingItem.PreviousLap.LapTime = parseFloatValue(standing, "LastTime");

                                if (standingItem.PreviousLap.LapTime <= 1)
                                {
                                    standingItem.PreviousLap.LapTime = standingItem.CurrentLap.LapTime;
                                }
                            }
                            */

                            if (SharedData.Sessions.SessionList[sessionIndex].Type == SharedData.Sessions.CurrentSession.Type)
                            {
                                if ((standingItem.CurrentTrackPct % 1.0) > 0.1)
                                {
                                    standingItem.PreviousLap.Position = parseIntValue(standing, "Position");
                                    standingItem.PreviousLap.Gap = parseFloatValue(standing, "Time");
                                    standingItem.PreviousLap.GapLaps = parseIntValue(standing, "Lap");
                                    standingItem.CurrentLap.Position = parseIntValue(standing, "Position");
                                }
                            }

                            if (standingItem.Driver.CarIdx < 0)
                            {
                                // insert item
                                int driverIndex = SharedData.Drivers.FindIndex(d => d.CarIdx.Equals(carIdx));
                                standingItem.setDriver(carIdx);
                                standingItem.FastestLap = parseFloatValue(standing, "FastestTime");
                                standingItem.LapsLed = parseIntValue(standing, "LapsLed");
                                standingItem.CurrentTrackPct = parseFloatValue(standing, "LapsDriven");
                                standingItem.Laps = new List<LapInfo>();

                                LapInfo newLap = new LapInfo();
                                newLap.LapNum = parseIntValue(standing, "LapsComplete");
                                newLap.LapTime = parseFloatValue(standing, "LastTime");
                                newLap.Position = parseIntValue(standing, "Position");
                                newLap.Gap = parseFloatValue(standing, "Time");
                                newLap.GapLaps = parseIntValue(standing, "Lap");
                                newLap.SectorTimes = new List<Sector>(3);
                                standingItem.Laps.Add(newLap);

                                standingItem.CurrentLap = new LapInfo();
                                standingItem.CurrentLap.LapNum = parseIntValue(standing, "LapsComplete") + 1;
                                standingItem.CurrentLap.Position = parseIntValue(standing, "Position");
                                standingItem.CurrentLap.Gap = parseFloatValue(standing, "Time");
                                standingItem.CurrentLap.GapLaps = parseIntValue(standing, "Lap");

                                lock (SharedData.SharedDataLock)
                                {
                                    SharedData.Sessions.SessionList[sessionIndex].Standings.Add(standingItem);
                                    SharedData.Sessions.SessionList[sessionIndex].UpdatePosition();
                                }
                            }

                            int lapnum = parseIntValue(standing, "LapsComplete");
                            standingItem.FastestLap = parseFloatValue(standing, "FastestTime");
                            standingItem.LapsLed = parseIntValue(standing, "LapsLed");

                            if (SharedData.Sessions.SessionList[sessionIndex].Type == SharedData.Sessions.CurrentSession.Type)
                            {
                                standingItem.PreviousLap.LapTime = parseFloatValue(standing, "LastTime");
                            }

                            if (SharedData.Sessions.CurrentSession.State == SessionStates.cooldown)
                            {
                                standingItem.CurrentLap.Gap = parseFloatValue(standing, "Time");
                                standingItem.CurrentLap.GapLaps = parseIntValue(standing, "Lap");
                                standingItem.CurrentLap.Position = parseIntValue(standing, "Position");
                                standingItem.CurrentLap.LapNum = parseIntValue(standing, "LapsComplete");
                            }

                            standingItem.Position = parseIntValue(standing, "Position");
                            standingItem.NotifySelf();
                            standingItem.NotifyLaps();

                            position++;
                        }
                    }

                    // update/add position for drivers not in results
                    foreach (DriverInfo driver in standingsDrivers)
                    {
                        StandingsItem standingItem = SharedData.Sessions.SessionList[sessionIndex].FindDriver(driver.CarIdx);
                        if (standingItem.Driver.CarIdx < 0)
                        {
                            if (SharedData.settings.IncludeMe || (!SharedData.settings.IncludeMe && standingItem.Driver.CarIdx != 63))
                            {
                                standingItem.setDriver(driver.CarIdx);
                                standingItem.Position = position;
                                standingItem.Laps = new List<LapInfo>();
                                lock (SharedData.SharedDataLock)
                                {
                                    SharedData.Sessions.SessionList[sessionIndex].Standings.Add(standingItem);
                                }
                                position++;
                            }
                        }
                        else if (!SharedData.settings.IncludeMe && driver.CarIdx < 63)
                        {
                            standingItem.Position = position;
                            position++;
                        }
                    }
                }
            }

            // add qualify session if it doesn't exist when race starts and fill it with YAML QualifyResultsInfo
            SessionInfo qualifySession = SharedData.Sessions.findSessionByType(SessionTypes.qualify);
            if (qualifySession.Type == SessionTypes.none)
            {
                qualifySession.Type = SessionTypes.qualify;

                length = yaml.Length;
                start = yaml.IndexOf("QualifyResultsInfo:\n", 0, length);

                // if found
                if (start >= 0)
                {
                    end = yaml.IndexOf("\n\n", start, length - start);

                    string QualifyResults = yaml.Substring(start, end - start);

                    length = QualifyResults.Length;
                    start = QualifyResults.IndexOf(" Results:\n", 0, length);
                    end = length;

                    string Results = QualifyResults.Substring(start, end - start - 1);
                    string[] resultList = Results.Split(new string[] { "\n - " }, StringSplitOptions.RemoveEmptyEntries);

                    qualifySession.FastestLap = float.MaxValue;

                    foreach (string result in resultList)
                    {
                        if (result != " Results:")
                        {
                            StandingsItem qualStandingsItem = qualifySession.FindDriver(parseIntValue(result, "CarIdx"));

                            if (qualStandingsItem.Driver.CarIdx > 0) // check if driver is in quali session
                            {
                                qualStandingsItem.Position = parseIntValue(result, "Position") + 1;
                            }
                            else // add driver to quali session
                            {
                                qualStandingsItem.setDriver(parseIntValue(result, "CarIdx"));
                                qualStandingsItem.Position = parseIntValue(result, "Position") + 1;
                                qualStandingsItem.FastestLap = parseFloatValue(result, "FastestTime");
                                lock (SharedData.SharedDataLock)
                                {
                                    qualifySession.Standings.Add(qualStandingsItem);
                                }
                                // update session fastest lap
                                if (qualStandingsItem.FastestLap < qualifySession.FastestLap && qualStandingsItem.FastestLap > 0)
                                    qualifySession.FastestLap = qualStandingsItem.FastestLap;
                            }
                        }
                    }
                    SharedData.Sessions.SessionList.Add(qualifySession); // add quali session
                }
            }

            // get qualify results if race session standings is empty
            foreach (SessionInfo session in SharedData.Sessions.SessionList)
            {
                if (session.Type == SessionTypes.race && session.Standings.Count < 1)
                {
                    length = yaml.Length;
                    start = yaml.IndexOf("QualifyResultsInfo:\n", 0, length);

                    // if found
                    if (start >= 0)
                    {
                        end = yaml.IndexOf("\n\n", start, length - start);

                        string QualifyResults = yaml.Substring(start, end - start);

                        length = QualifyResults.Length;
                        start = QualifyResults.IndexOf(" Results:\n", 0, length);
                        end = length;

                        string Results = QualifyResults.Substring(start, end - start - 1);
                        string[] resultList = Results.Split(new string[] { "\n - " }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string result in resultList)
                        {
                            if (result != " Results:")
                            {
                                StandingsItem standingItem = new StandingsItem();
                                standingItem.setDriver(parseIntValue(result, "CarIdx"));
                                standingItem.Position = parseIntValue(result, "Position") + 1;
                                lock (SharedData.SharedDataLock)
                                {
                                    session.Standings.Add(standingItem);
                                }
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
            bool haveNewCam = false;
            foreach (string camera in cameraList)
            {
                int cameraNum = parseIntValue(camera, "GroupNum");
                if (cameraNum < Int32.MaxValue)
                {
                    CameraGroup camgrp = SharedData.Camera.FindId(cameraNum);
                    if (camgrp.Id < 0)
                    {
                        CameraGroup cameraGroupItem = new CameraGroup();
                        cameraGroupItem.Id = cameraNum;
                        cameraGroupItem.Name = parseStringValue(camera, "GroupName");
                        lock (SharedData.SharedDataLock)
                        {
                            SharedData.Camera.Groups.Add(cameraGroupItem);
                            haveNewCam = true;
                        }
                    }
                }
            }
            if (SharedData.settings.CamerasButtonColumn && haveNewCam) // If we have a new cam and want Camera Buttons, then forece a refresh of the main window buttons
                SharedData.refreshButtons = true;

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

                // automagic sector selection
                if (SharedData.SelectedSectors.Count == 0)
                {
                    SharedData.SelectedSectors.Clear();

                    // load sectors
                    CfgFile sectorsIni = new CfgFile(Directory.GetCurrentDirectory() + "\\sectors.ini");
                    string sectorValue = sectorsIni.getValue("Sectors", SharedData.Track.Id.ToString(),false,String.Empty,false);
                    string[] selectedSectors = sectorValue.Split(';');
                    Array.Sort(selectedSectors);

                    SharedData.SelectedSectors.Clear();
                    if (sectorValue.Length > 0)
                    {
                        foreach (string sector in selectedSectors)
                        {
                            float number;
                            if (Single.TryParse(sector, out number))
                            {
                                SharedData.SelectedSectors.Add(number);
                            }
                        }
                    }
                    else
                    {
                        if (SharedData.Sectors.Count == 2)
                        {
                            foreach (float sector in SharedData.Sectors)
                                SharedData.SelectedSectors.Add(sector);
                        }
                        else
                        {
                            float prevsector = 0;
                            foreach (float sector in SharedData.Sectors)
                            {

                                if (sector == 0 && SharedData.SelectedSectors.Count == 0)
                                {
                                    SharedData.SelectedSectors.Add(sector);
                                }
                                else if (sector >= 0.333 && SharedData.SelectedSectors.Count == 1)
                                {
                                    if (sector - 0.333 < Math.Abs(prevsector - 0.333))
                                    {
                                        SharedData.SelectedSectors.Add(sector);
                                    }
                                    else
                                    {
                                        SharedData.SelectedSectors.Add(prevsector);
                                    }
                                }
                                else if (sector >= 0.666 && SharedData.SelectedSectors.Count == 2)
                                {
                                    if (sector - 0.666 < Math.Abs(prevsector - 0.666))
                                    {
                                        SharedData.SelectedSectors.Add(sector);
                                    }
                                    else
                                    {
                                        SharedData.SelectedSectors.Add(prevsector);
                                    }
                                }

                                prevsector = sector;
                            }
                        }
                    }
                }
            }
        }

        Double timeoffset = 0;
        Double currentime = 0;
        Double prevtime = 0;

        private Int32 currentOffset;

        public Int32 CurrentOffset
        {
            get { return currentOffset; }
            private set { currentOffset = value; }
        }
        

        public bool UpdateAPIData()
        {
            Single[] DriversTrackPct;
            Int32[] DriversLapNum;
            Int32[] DriversTrackSurface;
            //Int32 skip = 0;

            Boolean readCache = true;

            //Check if the SDK is connected
            if (sdk.IsConnected())
            {
                if (sdk.GetData("SessionNum") == null)
                {
                    return true; // No Data , but connection is alive
                }

                int newUpdate = sdk.Header.SessionInfoUpdate;
                if (newUpdate != lastUpdate)
                {
                    // wait and lock
                    SharedData.mutex.WaitOne();

                    Single trklen = SharedData.Track.Length;
                    lastUpdate = newUpdate;

                    parser(sdk.GetSessionInfo());

                    if (trklen != SharedData.Track.Length) // track changed, reload timedelta
                        SharedData.timedelta = new TimeDelta(SharedData.Track.Length, SharedData.settings.DeltaDistance, 64);
                    SharedData.mutex.ReleaseMutex();
                }

                if (readCache)
                {
                    //SharedData.readCache(SharedData.Sessions.SessionId);
                    readCache = false;
                }

                // when session changes, reset prevtime
                if (currentime >= prevtime)
                    currentime = (Double)sdk.GetData("SessionTime");
                else
                    prevtime = 0.0;

                // wait and lock
                SharedData.mutex.WaitOne();

                SharedData.Sessions.setCurrentSession((int)sdk.GetData("SessionNum"));
                SharedData.Sessions.CurrentSession.setFollowedDriver((int)sdk.GetData("CamCarIdx"));
                SharedData.Sessions.CurrentSession.FollowedDriver.AddAirTime(currentime - prevtime);
                if (currentime > prevtime)
                {
                    SharedData.Sessions.CurrentSession.Time = (Double)sdk.GetData("SessionTime");

                    // hide ui if needed
                    if (SharedData.showSimUi == false)
                    {
                        int currentCamState = (int)sdk.GetData("CamCameraState");
                        if ((currentCamState & 0x0008) == 0)
                        {
                            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSetState, (currentCamState | 0x0008), 0);
                        }
                    }

                    if (SharedData.Sessions.CurrentSession.SessionStartTime < 0)
                    {
                        SharedData.Sessions.CurrentSession.SessionStartTime = SharedData.Sessions.CurrentSession.Time;
                        SharedData.Sessions.CurrentSession.CurrentReplayPosition = (Int32)sdk.GetData("ReplayFrameNum");
                    }

                    // clear delta between sessions
                    if (SharedData.Sessions.CurrentSession.Time < 0.5)
                        SharedData.timedelta = new TimeDelta(SharedData.Track.Length, SharedData.settings.DeltaDistance, 64);

                    SharedData.Sessions.CurrentSession.TimeRemaining = (Double)sdk.GetData("SessionTimeRemain");

                    if (((Double)sdk.GetData("SessionTime") - (Double)sdk.GetData("ReplaySessionTime")) < 2)
                        timeoffset = (Int32)sdk.GetData("ReplayFrameNum") - ((Double)sdk.GetData("SessionTime") * 60);

                    SessionStates prevState = SharedData.Sessions.CurrentSession.State;
                    SharedData.Sessions.CurrentSession.State = (SessionStates)sdk.GetData("SessionState");
                   
                    if (prevState != SharedData.Sessions.CurrentSession.State)
                    {
                        SessionEvent ev = new SessionEvent(
                            SessionEventTypes.state,
                            (Int32)(((Double)sdk.GetData("SessionTime") * 60) + timeoffset),
                            SharedData.Sessions.CurrentSession.FollowedDriver.Driver,
                            "Session state changed to " + SharedData.Sessions.CurrentSession.State.ToString(),
                            SharedData.Sessions.CurrentSession.Type,
                            SharedData.Sessions.CurrentSession.LapsComplete
                        );
                        SharedData.Events.Add(ev);

                        // if state changes from racing to checkered update final lap
                        if (SharedData.Sessions.CurrentSession.Type == SessionTypes.race &&
                            SharedData.Sessions.CurrentSession.FinishLine == Int32.MaxValue &&
                            prevState == SessionStates.racing &&
                            SharedData.Sessions.CurrentSession.State == SessionStates.checkered)
                        {
                            SharedData.Sessions.CurrentSession.FinishLine = (Int32)Math.Ceiling(SharedData.Sessions.CurrentSession.getLeader().CurrentTrackPct);
                        }

                        // if new state is racing then trigger green flag
                        if (SharedData.Sessions.CurrentSession.State == SessionStates.racing && SharedData.Sessions.CurrentSession.Flag != SessionFlags.invalid)
                            SharedData.triggers.Push(TriggerTypes.flagGreen);
                        else if (SharedData.Sessions.CurrentSession.State == SessionStates.checkered ||
                            SharedData.Sessions.CurrentSession.State == SessionStates.cooldown)
                            SharedData.triggers.Push(TriggerTypes.flagCheckered);
                        // before race show yellows
                        else if (SharedData.Sessions.CurrentSession.State == SessionStates.gridding ||
                            SharedData.Sessions.CurrentSession.State == SessionStates.pacing ||
                            SharedData.Sessions.CurrentSession.State == SessionStates.warmup)
                            SharedData.triggers.Push(TriggerTypes.flagYellow);

                        if (SharedData.Sessions.CurrentSession.State == SessionStates.racing &&
                            (prevState == SessionStates.pacing || prevState == SessionStates.gridding))
                            timeoffset = (Int32)sdk.GetData("ReplayFrameNum") - ((Double)sdk.GetData("SessionTime") * 60);
                    }

                    SessionFlags prevFlag = SharedData.Sessions.CurrentSession.Flag;
                    SessionStartLights prevLight = SharedData.Sessions.CurrentSession.StartLight;

                    parseFlag(SharedData.Sessions.CurrentSession, (Int32)sdk.GetData("SessionFlags"));

                    // white flag handling
                    if (SharedData.Sessions.CurrentSession.LapsRemaining == 1 || SharedData.Sessions.CurrentSession.TimeRemaining <= 0)
                        SharedData.Sessions.CurrentSession.Flag = SessionFlags.white;

                    if (prevFlag != SharedData.Sessions.CurrentSession.Flag)
                    {
                        SessionEvent ev = new SessionEvent(
                            SessionEventTypes.flag,
                            (Int32)(((Double)sdk.GetData("SessionTime") * 60) + timeoffset),
                            SharedData.Sessions.CurrentSession.FollowedDriver.Driver,
                            SharedData.Sessions.CurrentSession.Flag.ToString() + " flag",
                            SharedData.Sessions.CurrentSession.Type,
                            SharedData.Sessions.CurrentSession.LapsComplete
                        );
                        SharedData.Events.Add(ev);

                        if (SharedData.Sessions.CurrentSession.State == SessionStates.racing)
                        {
                            switch (SharedData.Sessions.CurrentSession.Flag)
                            {
                                case SessionFlags.caution:
                                case SessionFlags.yellow:
                                case SessionFlags.yellowWaving:
                                    SharedData.triggers.Push(TriggerTypes.flagYellow);
                                    break;
                                case SessionFlags.checkered:
                                    SharedData.triggers.Push(TriggerTypes.flagCheckered);
                                    break;
                                case SessionFlags.white:
                                    if (SharedData.Sessions.CurrentSession.Type == SessionTypes.race) // White flag only in Races!
                                         SharedData.triggers.Push(TriggerTypes.flagWhite);
                                    break;
                                default:
                                    SharedData.triggers.Push(TriggerTypes.flagGreen);
                                    break;
                            }
                        }

                        // yellow manual calc
                        if (SharedData.Sessions.CurrentSession.Flag == SessionFlags.yellow)
                        {
                            SharedData.Sessions.CurrentSession.Cautions++;
                        }
                    }

                    if (prevLight != SharedData.Sessions.CurrentSession.StartLight)
                    {

                        switch (SharedData.Sessions.CurrentSession.StartLight)
                        {
                            case SessionStartLights.off:
                                SharedData.triggers.Push(TriggerTypes.lightsOff);
                                break;
                            case SessionStartLights.ready:
                                SharedData.triggers.Push(TriggerTypes.lightsReady);
                                break;
                            case SessionStartLights.set:
                                SharedData.triggers.Push(TriggerTypes.lightsSet);
                                break;
                            case SessionStartLights.go:
                                SharedData.triggers.Push(TriggerTypes.lightsGo);
                                break;
                        }

                        SessionEvent ev = new SessionEvent(
                            SessionEventTypes.startlights,
                            (Int32)(((Double)sdk.GetData("SessionTime") * 60) + timeoffset),
                            SharedData.Sessions.CurrentSession.FollowedDriver.Driver,
                            "Start lights changed to " + SharedData.Sessions.CurrentSession.StartLight.ToString(),
                            SharedData.Sessions.CurrentSession.Type,
                            SharedData.Sessions.CurrentSession.LapsComplete
                        );
                        SharedData.Events.Add(ev);
                    }

                    SharedData.Camera.CurrentGroup = (Int32)sdk.GetData("CamGroupNumber");

                    if ((SharedData.currentFollowedDriver != SharedData.Sessions.CurrentSession.FollowedDriver.Driver.NumberPlatePadded)
                        || (SharedData.Camera.CurrentGroup != SharedData.currentCam))
                    {

                        SharedData.currentFollowedDriver = SharedData.Sessions.CurrentSession.FollowedDriver.Driver.NumberPlatePadded;
                        SharedData.currentCam = SharedData.Camera.CurrentGroup;
                        logger.Debug("CAMSWITCH BY SIM CAM {0} DRiver {1}", SharedData.currentCam, SharedData.currentFollowedDriver);
                        if (iRTVOConnection.isServer)
                        {
                            iRTVOConnection.BroadcastMessage("SWITCH", SharedData.currentFollowedDriver, SharedData.currentCam);
                        }
                    }

                    DriversTrackPct = (Single[])sdk.GetData("CarIdxLapDistPct");
                    DriversLapNum = (Int32[])sdk.GetData("CarIdxLap");
                    DriversTrackSurface = (Int32[])sdk.GetData("CarIdxTrackSurface");

                    // The Voice-Chat Stuff
                    int newRadioTransmitCaridx = (Int32)sdk.GetData("RadioTransmitCarIdx");
                    if (newRadioTransmitCaridx != SharedData.currentRadioCarIdx)
                    {
                        // TODO: Add Logging!
                        // System.Diagnostics.Trace.WriteLine("Radio Old = " + SharedData.currentRadioTransmitcarIdx + " New = " + newRadioTransmitCaridx);
                        if (newRadioTransmitCaridx == -1)
                            SharedData.triggers.Push(TriggerTypes.radioOff);
                        else
                            SharedData.triggers.Push(TriggerTypes.radioOn);
                        SharedData.currentRadioCarIdx = newRadioTransmitCaridx;
                    }

                    if (((Double)sdk.GetData("SessionTime") - (Double)sdk.GetData("ReplaySessionTime")) > 1.1)
                        SharedData.inReplay = true;
                    else
                        SharedData.inReplay = false;

                    for (Int32 i = 0; i <= Math.Min(64, SharedData.Drivers.Count); i++)
                    {
                        StandingsItem driver = SharedData.Sessions.CurrentSession.FindDriver(i);
                        Double prevpos = driver.PrevTrackPct;
                        Double prevupdate = driver.PrevTrackPctUpdate;
                        Double curpos = DriversTrackPct[i];

                        if (currentime > prevupdate && curpos != prevpos)
                        {
                             Single speed = 0;

                            // calculate speed
                            if (curpos < 0.1 && prevpos > 0.9) // crossing s/f line
                            {
                                speed = (Single)((((curpos - prevpos) + 1) * (Double)SharedData.Track.Length) / (currentime - prevupdate));
                            }
                            else
                            {
                                speed = (Single)(((curpos - prevpos) * (Double)SharedData.Track.Length) / (currentime - prevupdate));
                            }

                            if (Math.Abs(driver.Prevspeed - speed) < 1 && (curpos - prevpos) >= 0) // filter junk
                            {
                                driver.Speed = speed;
                            }

                            driver.Prevspeed = speed;
                            driver.PrevTrackPct = curpos;
                            driver.PrevTrackPctUpdate = currentime;

                            // update track position
                            if (driver.Finished == false && (SurfaceTypes)DriversTrackSurface[i] != SurfaceTypes.NotInWorld)
                            {
                                driver.CurrentTrackPct = DriversLapNum[i] + DriversTrackPct[i] - 1;
                            }

                            // add new lap
                            if (curpos < 0.1 && prevpos > 0.9 && driver.Finished == false) // crossing s/f line
                            {
                                if ((SurfaceTypes)DriversTrackSurface[i] != SurfaceTypes.NotInWorld &&
                                    speed > 0)
                                {
                                    Double now = currentime - ((curpos / (1 + curpos - prevpos)) * (currentime - prevtime));

                                    Sector sector = new Sector();
                                    sector.Num = driver.Sector;
                                    sector.Speed = driver.Speed;
                                    sector.Time = (Single)(now - driver.SectorBegin);
                                    sector.Begin = driver.SectorBegin;

                                    driver.CurrentLap.SectorTimes.Add(sector);
                                    driver.CurrentLap.LapTime = (Single)(now - driver.Begin);
                                    driver.CurrentLap.ClassPosition = SharedData.Sessions.CurrentSession.getClassPosition(driver.Driver);
                                    if (SharedData.Sessions.CurrentSession.Type == SessionTypes.race)
                                        driver.CurrentLap.Gap = (Single)driver.GapLive;
                                    else
                                        driver.CurrentLap.Gap = driver.CurrentLap.LapTime - SharedData.Sessions.CurrentSession.FastestLap;
                                    driver.CurrentLap.GapLaps = 0;


                                    if (driver.CurrentLap.LapNum > 0 &&
                                        driver.Laps.FindIndex(l => l.LapNum.Equals(driver.CurrentLap.LapNum)) == -1 &&
                                        (SharedData.Sessions.CurrentSession.State != SessionStates.gridding ||
                                        SharedData.Sessions.CurrentSession.State != SessionStates.cooldown))
                                    {
                                        driver.Laps.Add(driver.CurrentLap);
                                    }

                                    driver.CurrentLap = new LapInfo();
                                    driver.CurrentLap.LapNum = DriversLapNum[i];
                                    driver.CurrentLap.Gap = driver.PreviousLap.Gap;
                                    driver.CurrentLap.GapLaps = driver.PreviousLap.GapLaps;
                                    driver.CurrentLap.ReplayPos = (Int32)(((Double)sdk.GetData("SessionTime") * 60) + timeoffset);
                                    driver.CurrentLap.SessionTime = SharedData.Sessions.CurrentSession.Time;
                                    driver.SectorBegin = now;
                                    driver.Sector = 0;
                                    driver.Begin = now;

                                    // caution lap calc
                                    if (SharedData.Sessions.CurrentSession.Flag == SessionFlags.yellow && driver.Position == 1)
                                        SharedData.Sessions.CurrentSession.CautionLaps++;

                                    // class laps led
                                    if (SharedData.Sessions.CurrentSession.getClassLeader(driver.Driver.CarClassName).Driver.CarIdx == driver.Driver.CarIdx && driver.CurrentLap.LapNum > 1)
                                        driver.ClassLapsLed = driver.ClassLapsLed + 1;
                                }
                            }

                            // add sector times
                            if (SharedData.SelectedSectors.Count > 0 && driver.Driver.CarIdx >= 0)
                            {
                                for (int j = 0; j < SharedData.SelectedSectors.Count; j++)
                                {
                                    if (curpos > SharedData.SelectedSectors[j] && j > driver.Sector)
                                    {
                                        Double now = currentime - ((curpos - SharedData.SelectedSectors[j]) * (curpos - prevpos));
                                        Sector sector = new Sector();
                                        sector.Num = driver.Sector;
                                        sector.Time = (Single)(now - driver.SectorBegin);
                                        sector.Speed = driver.Speed;
                                        sector.Begin = driver.SectorBegin;
                                        driver.CurrentLap.SectorTimes.Add(sector);
                                        driver.SectorBegin = now;
                                        driver.Sector = j;
                                    }
                                }
                            }

                            // cross finish line
                            if (driver.CurrentLap.LapNum + driver.CurrentLap.GapLaps >= SharedData.Sessions.CurrentSession.FinishLine &&
                                (SurfaceTypes)DriversTrackSurface[i] != SurfaceTypes.NotInWorld &&
                                SharedData.Sessions.CurrentSession.Type == SessionTypes.race &&
                                driver.Finished == false)
                            {
                                // finishing the race
                                SharedData.Sessions.CurrentSession.UpdatePosition();
                                driver.CurrentTrackPct = (Math.Floor(driver.CurrentTrackPct) + 0.0064) - (0.0001 * driver.Position);
                                driver.Finished = true;
                            }

                            // add events
                            if (driver.Driver.CarIdx >= 0)
                            {
                                // update tracksurface
                                driver.TrackSurface = (SurfaceTypes)DriversTrackSurface[i];
                                driver.NotifyPosition();
                            }
                        }
                    }

                    SharedData.timedelta.Update(currentime, DriversTrackPct);
                    SharedData.Sessions.CurrentSession.UpdatePosition();

                    prevtime = currentime;
                    SharedData.currentSessionTime = currentime;

                    SharedData.apiConnected = true;
                    if (SharedData.theme != null)
                        SharedData.runOverlay = true;
                }

                SharedData.mutex.ReleaseMutex();

                SharedData.scripting.ApiTick(this);

                System.Threading.Thread.Sleep(4);
                return true;
            }
            else if (sdk.IsInitialized)
            {
                sdk.Shutdown();
                lastUpdate = -1;
                return false;
            }
            else
                return false;
        }

        public int NextConnectTry = Environment.TickCount;

        bool Interfaces.ISimulationAPI.IsConnected
        {
            get { return sdk.IsInitialized && sdk.IsConnected(); }
        }

        bool Interfaces.ISimulationAPI.ConnectAPI()
        {
            try
            {
                return sdk.Startup();
            }
            catch
            {
                return false;
            }
        }

        void IDisposable.Dispose()
        {
            sdk.Shutdown();
        }


        public object GetData(string key)
        {
            return sdk.GetData(key);
        }

        public void HideUI()
        {

        }

        public void SwitchCamera(int driver, int camera)
        {
            logger.Trace("SwitchCamera dr {0} cam {1}", driver, camera);
            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, driver, camera);
        }

        public void ReplaySetPlaySpeed(int playspeed, int slowmotion)
        {
            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, playspeed, slowmotion);
        }

        public void ReplaySetPlayPosition(Interfaces.ReplayPositionModeTypes mode, int position)
        {
            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlayPosition, (int)mode, position);
        }

        public void ReplaySearch(Interfaces.ReplaySearchModeTypes mode, int position)
        {
            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySearch, (int)mode, 0);
        }

        public void Pause()
        {
            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 0, 0);
        }

        public void Play()
        {
            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
        }
    }
}
