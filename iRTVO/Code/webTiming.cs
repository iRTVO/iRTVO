using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// additional
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.IO.Compression;
using NLog;
using iRTVO.Data;
using iRTVO.Interfaces;

namespace iRTVO.WebTiming
{
    public class webTiming
    {
        // Logging
        private static Logger logger = LogManager.GetCurrentClassLogger();

        struct webtimingDriver
        {
            public string position;
            public string number;
            public string name;
            public string gap;
            public string interval;
            public string previouslap;
            public string fastestlap;
            public string pit;
            public string lapsled;
            public string lap;
            public string car;  // KJ: car make and model
            public bool pitting; // KJ: in pits?
            public bool retired;

            public string[] sectors;
            public string classname;
            public string classid;
            public string classposition;
            public string classgap;
            public string classinterval;

            public webtimingDriver(StandingsItem driver, SessionInfo session)
            {
                position = driver.PositionLive.ToString();
                name = driver.Driver.Name;

                // KJ: for multi-car/multi-class races and pitting status
                car = "";
                pitting = false;

                // KJ: set name for team events
                if ( driver.Driver.TeamId > 0 )
                {
                    name = driver.Driver.TeamName + " (" + driver.Driver.Shortname + ")";
                }
                number = driver.Driver.NumberPlate;
                car = SharedData.theme.getCar(driver.Driver.CarId);  // KJ: set car
                lap = driver.CurrentLap.LapNum.ToString();
                fastestlap =Utils.floatTime2String(driver.FastestLap, 3, false);
                previouslap =Utils.floatTime2String(driver.PreviousLap.LapTime, 3, false);
                pit = driver.PitStops.ToString();
                lapsled = driver.LapsLed.ToString();
                sectors = new string[0];

                classid = driver.Driver.CarClass.ToString();
                classname = driver.Driver.CarClassName;
                classposition = session.getClassPosition(driver.Driver).ToString();
                classgap = driver.ClassGapLive_HR;
                classinterval = driver.ClassIntervalLive_HR;

                StandingsItem leader = SharedData.Sessions.CurrentSession.FindPosition(1, DataOrders.liveposition);
                StandingsItem infront;

                if(driver.PositionLive <= 1) {
                    infront = new StandingsItem();
                }
                else {
                    infront = SharedData.Sessions.CurrentSession.FindPosition(driver.Position - 1, DataOrders.liveposition);
                }

                if (SharedData.Sessions.CurrentSession.Type == SessionTypes.race
                    /* && (driver.Finished == true || driver.Sector == 0) */)
                {
                    if (infront.PreviousLap.GapLaps > driver.PreviousLap.GapLaps)
                    {
                        interval = (infront.FindLap(driver.PreviousLap.LapNum).LapNum - driver.PreviousLap.LapNum) + " L";
                    }
                    else
                    {
                        interval =Utils.floatTime2String((driver.PreviousLap.Gap - infront.FindLap(driver.PreviousLap.LapNum).Gap), 3, false);
                    }

                    if (driver.PreviousLap.GapLaps > 0)
                    {
                        gap = driver.PreviousLap.GapLaps +" L";
                    }
                    else
                    {
                        gap =Utils.floatTime2String(driver.PreviousLap.Gap, 3, false);
                    }
                }
                else
                {
                    interval =Utils.floatTime2String((driver.FastestLap - infront.FastestLap), 3, false);
                    gap =Utils.floatTime2String((driver.FastestLap - leader.FastestLap), 3, false);
                }

                if (SharedData.SelectedSectors.Count > 0)
                {
                    sectors = new string[SharedData.SelectedSectors.Count];

                    for (int i = 0; i < SharedData.SelectedSectors.Count; i++)
                    {
                        if (driver.Sector <= 0) // first sector, show previous lap times
                        {
                            if (i < driver.PreviousLap.SectorTimes.Count)
                            {
                                Sector sector = driver.PreviousLap.SectorTimes.Find(s => s.Num.Equals(i));
                                if(sector != null)
                                {
                                    sectors[i] =Utils.floatTime2String(sector.Time, 1, false);
                                }
                                else
                                {
                                    sectors[i] = "-.--";
                                }
                            }
                            else
                            {
                                sectors[i] = "-.--";
                            }
                        }
                        else
                        {
                            if (i < driver.CurrentLap.SectorTimes.Count)
                            {
                                Sector sector = driver.CurrentLap.SectorTimes.Find(s => s.Num.Equals(i));
                                if(sector != null)
                                {
                                    sectors[i] =Utils.floatTime2String(sector.Time, 1, false);
                                }
                                else
                                {
                                    sectors[i] = "-.--";
                                }
                            }
                            else
                            {
                                sectors[i] = "-.--";
                            }
                        }
                    }
                }

                if (SharedData.Sessions.CurrentSession.Type == SessionTypes.race &&
                    driver.TrackSurface == SurfaceTypes.NotInWorld && 
                SharedData.allowRetire &&
                (SharedData.Sessions.CurrentSession.Time - driver.OffTrackSince) > 1)
                {
                    retired = true;

                    if (infront.CurrentLap.LapNum > driver.CurrentLap.LapNum)
                    {
                        interval = (infront.CurrentLap.LapNum - driver.CurrentLap.LapNum) + " L";
                    }
                    else
                    {
                        interval =Utils.floatTime2String((driver.PreviousLap.Gap - infront.PreviousLap.Gap), 3, false);
                    }
                    if ((leader.CurrentLap.LapNum - driver.CurrentLap.LapNum) > 0)
                    {
                        gap = leader.CurrentLap.LapNum - driver.CurrentLap.LapNum + " L";
                    }
                    else
                    {
                        gap =Utils.floatTime2String(driver.PreviousLap.Gap, 3, false);
                    }
                }
                else
                {
                    retired = false;
                }
                // KJ: set pitting status
                if (driver.TrackSurface == SurfaceTypes.InPitStall)
                {
                    pitting = true;
                }
                else
                {
                    pitting = false;
                }
            }
        }

        struct webTimingObject
        {
            public long timestamp;
            public string trackname;
            public string sessiontype;
            public string sessionstate;
            public float timeremaining;
            public int currentlap;
            public int totallaps;
            public string sessionflag;
            public int cautions;
            public int cautionlaps;
            public string fastestlap;
            public string fastestdriver;
            public int fastestlapnum;

            public webtimingDriver[] drivers;

            public TelemData tracker;
        }

        public class TrackerDriver
        {
            public TrackerDriver(int idx,string name, string carnum, float distance, bool onpit)
            {
                Index = idx;
                Name = name;
                CarNum = carnum;
                LapPct = distance;
                OnPitRoad = onpit;
            }

            public int Index { get; set; }

            public string Name { get; set; }

            public string CarNum { get; set; }

            public float LapPct { get; set; }

            public bool OnPitRoad { get; set; }
        }

        struct TelemData
        {
            public Dictionary<string,float> drivers;
            public Int32 trackId;            
        }
        
        private string postURL;

        public webTiming(string url) {
            postURL = url;
        }

        public void postData(object o)
        {
            // wait
            SharedData.mutex.WaitOne();
            DateTime mutexLocked = DateTime.Now;

            webTimingObject data = new webTimingObject();
            data.timestamp = (long)( DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond );
            data.trackname = SharedData.Track.Name;
            data.sessiontype = SharedData.Sessions.CurrentSession.Type.ToString();
            data.sessionstate = SharedData.Sessions.CurrentSession.State.ToString();

            if (SharedData.Sessions.CurrentSession.State == SessionStates.checkered ||
                SharedData.Sessions.CurrentSession.State == SessionStates.cooldown)
            {
                data.sessionflag = SessionFlags.checkered.ToString();
            }
            else 
            {
                data.sessionflag = SharedData.Sessions.CurrentSession.Flag.ToString();
            }

            data.currentlap = SharedData.Sessions.CurrentSession.LapsComplete;
            data.totallaps = SharedData.Sessions.CurrentSession.LapsTotal;
            data.timeremaining = (float)SharedData.Sessions.CurrentSession.TimeRemaining;
            data.cautions = SharedData.Sessions.CurrentSession.Cautions;
            data.cautionlaps = SharedData.Sessions.CurrentSession.CautionLaps;
            data.fastestlap =Utils.floatTime2String(SharedData.Sessions.CurrentSession.FastestLap, 3, true);
            data.fastestdriver = SharedData.Sessions.CurrentSession.FastestLapDriver.Name;
            data.fastestlapnum = SharedData.Sessions.CurrentSession.FastestLapNum;

            data.drivers = new webtimingDriver[SharedData.Sessions.CurrentSession.Standings.Count];
            data.tracker = new TelemData();
            data.tracker.trackId = SharedData.Track.Id;
            data.tracker.drivers = new Dictionary<string, float>();

            IEnumerable<StandingsItem> query = SharedData.Sessions.CurrentSession.Standings.OrderBy(s => s.PositionLive);

            int i = 0;
            foreach (StandingsItem si in query)
            {
                data.drivers[i] = new webtimingDriver(si, SharedData.Sessions.CurrentSession);
                if (si.TrackSurface != SurfaceTypes.NotInWorld)
                {
                    data.tracker.drivers[si.Driver.NumberPlate] = (float)si.TrackPct;
                    /*new TrackerDriver(si.Driver.CarIdx, si.Driver.Name, si.Driver.NumberPlate, (float)si.TrackPct,
                         (si.TrackSurface == SurfaceTypes.AproachingPits)
                         || (si.TrackSurface == SurfaceTypes.InPitStall));
                     * */
                }
                i++;
            }

            SharedData.mutex.ReleaseMutex();
            send(JsonConvert.SerializeObject(data));
            
        }


        /// <summary>
        /// method for validating a url with regular expressions
        /// </summary>
        /// <param name="url">url we're validating</param>
        /// <returns>true if valid, otherwise false</returns>
        public static bool isValidUrl(ref string url)
        {
            string pattern = @"^(http|https|ftp)\://[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*[^\.\,\)\(\s]$";
            Regex reg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return reg.IsMatch(url);
        }

        public Boolean send(string postData)
        {
            //logger.Trace("####{0}#####", postData);
            
            if (isValidUrl(ref postURL))
            {
                // Create a request using a URL that can receive a post.
                WebRequest request = WebRequest.Create(postURL);

                // Set the Method property of the request to POST.
                request.Method = "POST";

                // Create POST data and convert it to a byte array.
                string postString = "key=" + SharedData.settings.WebTimingPassword + "&sessionid=" + SharedData.Sessions.SessionId.ToString() + "&subsessionid=" + SharedData.Sessions.SubSessionId.ToString() + "&sessionnum=" + SharedData.Sessions.CurrentSession.Id.ToString() + "&type=" + SharedData.Sessions.CurrentSession.Type.ToString();

                /*
                if (SharedData.settings.webTimingCompression)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(postData);
                    var memoryStream = new MemoryStream();
                    using (var gZipStream = new DeflateStream(memoryStream, CompressionMode.Compress, true))
                    {
                        gZipStream.Write(buffer, 0, buffer.Length);
                    }

                    memoryStream.Position = 0;

                    var compressedData = new byte[memoryStream.Length];
                    memoryStream.Read(compressedData, 0, compressedData.Length);

                    //var gZipBuffer = new byte[compressedData.Length];
                    //Buffer.BlockCopy(compressedData, 0, gZipBuffer, 0, compressedData.Length);
                    //Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);

                    postString += "&compression=true&data=" + Convert.ToBase64String(compressedData);
                    Console.WriteLine(postString);
                    //Console.WriteLine("WebTiming compression: "+ (postData.Length * sizeof(char)).ToString() +"/"+ (deflateBuffer.Length * sizeof(byte)).ToString() );
                }
                else
                */
                    postString += "&data=" + postData;

                byte[] byteArray = Encoding.UTF8.GetBytes(postString);


                // Set the ContentType property of the WebRequest.
                request.ContentType = "application/x-www-form-urlencoded";

                // Set the ContentLength property of the WebRequest.
                request.ContentLength = byteArray.Length;

                // Get the request stream.
                Stream dataStream = new MemoryStream();
                try
                {
                    dataStream = request.GetRequestStream();
                    // Write the data to the request stream.
                    dataStream.Write(byteArray, 0, byteArray.Length);

                    SharedData.webBytes += byteArray.Length;

                    // Close the Stream object.
                    dataStream.Close();
                }
                catch (WebException ex)
                {
                    SharedData.webError += "\n" + DateTime.Now.ToString("s") + " " + ex.Message;
                    return false;
                }

                // Get the response.
                WebResponse response;
                try
                {
                    response = request.GetResponse();
                }
                catch (WebException ex)
                {
                    SharedData.webError += "\n" + DateTime.Now.ToString("s") + " " + ex.Message;
                    response = ex.Response as HttpWebResponse;
                    return false;
                }

                // Get the stream containing content returned by the server.
                try
                {
                    dataStream = response.GetResponseStream();
                }
                catch
                {
                    SharedData.webError += "\n" + DateTime.Now.ToString("s") + " Timeout";
                    return false;
                }

                // Open the stream using a StreamReader for easy access.
                try
                {
                    StreamReader reader = new StreamReader(dataStream);

                    // Read the content.
                    string responseFromServer = reader.ReadToEnd();

                    // Display the content.
                    if (responseFromServer.Length > 0)
                    {
                        SharedData.webError += "\n" + DateTime.Now.ToString("s") + " " + responseFromServer;
                    }

                    reader.Close();
                }
                catch
                {
                    SharedData.webError += "\n" + DateTime.Now.ToString("s") + " Unable to read response";
                    return false;
                }

                // Clean up the streams.
                dataStream.Close();
                response.Close();

                return true;
            }
            else
            {
                logger.Error("url invalid {0}", postURL);
                return false;
            }
        }
    }
}
