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

namespace iRTVO
{
    class webTiming
    {
        struct webtimingDriver
        {
            public string position;
            public string name;
            public string number;
            public string lap;
            public string fastestlap;
            public string previouslap;
            public string interval;
            public string gap;
            public string[] sectors;
            public string pit;
            public string lapsled;
            public bool retired;

            public string classname;
            public string classid;
            public string classposition;
            public string classgap;
            public string classinterval;

            public webtimingDriver(Sessions.SessionInfo.StandingsItem driver, Sessions.SessionInfo session)
            {
                position = driver.Position.ToString();
                name = driver.Driver.Name;
                number = driver.Driver.NumberPlate;
                lap = driver.CurrentLap.LapNum.ToString();
                fastestlap = iRTVO.Overlay.floatTime2String(driver.FastestLap, 3, false);
                previouslap = iRTVO.Overlay.floatTime2String(driver.PreviousLap.LapTime, 3, false);
                pit = driver.PitStops.ToString();
                lapsled = driver.LapsLed.ToString();
                sectors = new string[0];

                classid = driver.Driver.CarClass.ToString();
                classname = driver.Driver.CarClassName;
                classposition = session.getClassPosition(driver.Driver).ToString();
                classgap = driver.ClassGapLive_HR;
                classinterval = driver.ClassIntervalLive_HR;

                Sessions.SessionInfo.StandingsItem leader = SharedData.Sessions.CurrentSession.FindPosition(1, dataorder.position);
                Sessions.SessionInfo.StandingsItem infront;

                if(driver.Position <= 1) {
                    infront = new Sessions.SessionInfo.StandingsItem();
                }
                else {
                    infront = SharedData.Sessions.CurrentSession.FindPosition(driver.Position - 1, dataorder.position);
                }

                if (SharedData.Sessions.CurrentSession.Type == Sessions.SessionInfo.sessionType.race
                    /* && (driver.Finished == true || driver.Sector == 0) */)
                {
                    if (infront.PreviousLap.GapLaps > driver.PreviousLap.GapLaps)
                    {
                        interval = (infront.FindLap(driver.PreviousLap.LapNum).LapNum - driver.PreviousLap.LapNum) + " L";
                    }
                    else
                    {
                        interval = iRTVO.Overlay.floatTime2String((driver.PreviousLap.Gap - infront.FindLap(driver.PreviousLap.LapNum).Gap), 3, false);
                    }

                    if (driver.PreviousLap.GapLaps > 0)
                    {
                        gap = driver.PreviousLap.GapLaps +" L";
                    }
                    else
                    {
                        gap = iRTVO.Overlay.floatTime2String(driver.PreviousLap.Gap, 3, false);
                    }
                }
                else
                {
                    interval = iRTVO.Overlay.floatTime2String((driver.FastestLap - infront.FastestLap), 3, false);
                    gap = iRTVO.Overlay.floatTime2String((driver.FastestLap - leader.FastestLap), 3, false);
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
                                iRTVO.LapInfo.Sector sector = driver.PreviousLap.SectorTimes.Find(s => s.Num.Equals(i));
                                if(sector != null)
                                {
                                    sectors[i] = iRTVO.Overlay.floatTime2String(sector.Time, 1, false);
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
                                iRTVO.LapInfo.Sector sector = driver.CurrentLap.SectorTimes.Find(s => s.Num.Equals(i));
                                if(sector != null)
                                {
                                    sectors[i] = iRTVO.Overlay.floatTime2String(sector.Time, 1, false);
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

                if (SharedData.Sessions.CurrentSession.Type == Sessions.SessionInfo.sessionType.race &&
                    driver.TrackSurface == Sessions.SessionInfo.StandingsItem.SurfaceType.NotInWorld && 
                SharedData.allowRetire && 
                (DateTime.Now - driver.OffTrackSince).TotalSeconds > 1)
                {
                    retired = true;
                    if (infront.CurrentLap.LapNum > driver.CurrentLap.LapNum)
                    {
                        interval = (infront.CurrentLap.LapNum - driver.CurrentLap.LapNum) + " L";
                    }
                    else
                    {
                        interval = iRTVO.Overlay.floatTime2String((driver.PreviousLap.Gap - infront.PreviousLap.Gap), 3, false);
                    }
                    if ((leader.CurrentLap.LapNum - driver.CurrentLap.LapNum) > 0)
                    {
                        gap = leader.CurrentLap.LapNum - driver.CurrentLap.LapNum + " L";
                    }
                    else
                    {
                        gap = iRTVO.Overlay.floatTime2String(driver.PreviousLap.Gap, 3, false);
                    }
                }
                else
                {
                    retired = false;
                }
            }
        }

        struct webTimingObject
        {
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
        }
        
        private string postURL;

        public webTiming(string url) {
            postURL = url;

        }

        public void postData(object o)
        {
            // wait
            SharedData.writeMutex.WaitOne(1000);
            SharedData.readMutex.WaitOne(1000);

            webTimingObject data = new webTimingObject();

            data.trackname = SharedData.Track.name;
            data.sessiontype = SharedData.Sessions.CurrentSession.Type.ToString();
            data.sessionstate = SharedData.Sessions.CurrentSession.State.ToString();

            if (SharedData.Sessions.CurrentSession.State == Sessions.SessionInfo.sessionState.checkered ||
                SharedData.Sessions.CurrentSession.State == Sessions.SessionInfo.sessionState.cooldown)
            {
                data.sessionflag = Sessions.SessionInfo.sessionFlag.checkered.ToString();
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
            data.fastestlap = iRTVO.Overlay.floatTime2String(SharedData.Sessions.CurrentSession.FastestLap, 3, true);
            data.fastestdriver = SharedData.Sessions.CurrentSession.FastestLapDriver.Name;
            data.fastestlapnum = SharedData.Sessions.CurrentSession.FastestLapNum;

            data.drivers = new webtimingDriver[SharedData.Sessions.CurrentSession.Standings.Count];

            IEnumerable<Sessions.SessionInfo.StandingsItem> query = SharedData.Sessions.CurrentSession.Standings.OrderBy(s => s.Position);

            int i = 0;
            foreach (Sessions.SessionInfo.StandingsItem si in query)
            {
                data.drivers[i] = new webtimingDriver(si, SharedData.Sessions.CurrentSession);
                i++;
            }

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

        public void send(string postData)
        {
            if (isValidUrl(ref postURL))
            {
                // Create a request using a URL that can receive a post.
                WebRequest request = WebRequest.Create(postURL);

                // Set the Method property of the request to POST.
                request.Method = "POST";

                // Create POST data and convert it to a byte array.
                string postString = "key=" + Properties.Settings.Default.webTimingKey + "&sessionid=" + SharedData.Sessions.SessionId.ToString() + "&subsessionid=" + SharedData.Sessions.SubSessionId.ToString() + "&sessionnum=" + SharedData.Sessions.CurrentSession.Id.ToString() + "&type=" + SharedData.Sessions.CurrentSession.Type.ToString();

                /*
                if (Properties.Settings.Default.webTimingCompression)
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
                }

                // Get the stream containing content returned by the server.
                try
                {
                    dataStream = response.GetResponseStream();
                }
                catch
                {
                    SharedData.webError += "\n" + DateTime.Now.ToString("s") + " Timeout";
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
                }

                // Clean up the streams.
                dataStream.Close();
                response.Close();
            }
        }
    }
}
