using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// additional
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace iRTVO
{
    class webTiming
    {
        //webTimingObject data;

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

            public webtimingDriver(Sessions.SessionInfo.StandingsItem driver)
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

                Sessions.SessionInfo.StandingsItem leader = SharedData.Sessions.CurrentSession.FindPosition(1);
                Sessions.SessionInfo.StandingsItem infront;

                if(driver.Position <= 1) {
                    infront = new Sessions.SessionInfo.StandingsItem();
                }
                else {
                    infront = SharedData.Sessions.CurrentSession.FindPosition(driver.Position - 1);
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
                /*
                else if (SharedData.Sessions.CurrentSession.Type == Sessions.SessionInfo.sessionType.race &&
                    driver.Finished == false)
                {

                    if (infront.PreviousLap.LapNum > driver.PreviousLap.LapNum && infront.CurrentTrackPct - driver.CurrentTrackPct > 1)
                    {
                        interval = (infront.PreviousLap.LapNum - driver.PreviousLap.LapNum) + " L";
                    }
                    else
                    {
                        /*
                        if (SharedData.Sectors.Count > 0 && infront.FindLap(driver.CurrentLap.LapNum).SectorTimes.Count > 0)
                        {
                            DateTime infrontsector = infront.FindLap(driver.CurrentLap.LapNum).SectorTimes.Find(s => s.Num.Equals(driver.Sector - 1)).Begin;
                            DateTime mysector = driver.CurrentLap.SectorTimes.Find(s => s.Num.Equals(driver.Sector - 1)).Begin;
                            interval = iRTVO.Overlay.floatTime2String((float)(infrontsector - mysector).TotalSeconds, 3, false);
                        }
                        else
                        {
                        
                            interval = iRTVO.Overlay.floatTime2String((infront.PreviousLap.Gap - driver.PreviousLap.Gap), 3, false);
                        //}
                       
                    }

                    if (driver.PreviousLap.GapLaps > 0)
                    {
                        gap = driver.PreviousLap.GapLaps + " L";
                    }
                    else
                    {

                        /*if (leader.FindLap(driver.CurrentLap.LapNum).SectorTimes.Count > 0)
                        {
                            DateTime leadersector = leader.FindLap(driver.CurrentLap.LapNum).SectorTimes.Find(s => s.Num.Equals(driver.Sector - 1)).Begin;
                            DateTime mysector = driver.CurrentLap.SectorTimes.Find(s => s.Num.Equals(driver.Sector - 1)).Begin;
                            //gap = iRTVO.Overlay.floatTime2String(driver.PreviousLap.Gap, 3, false);
                            gap = iRTVO.Overlay.floatTime2String((float)(leadersector - mysector).TotalSeconds, 3, false);
                        }
                        else
                        {
                        
                            gap = gap = iRTVO.Overlay.floatTime2String(driver.PreviousLap.Gap, 3, false);
                        //}
                    }
                }
                */
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
                                try 
                                {
                                    sectors[i] = iRTVO.Overlay.floatTime2String(driver.PreviousLap.SectorTimes.First(s => s.Num.Equals(i)).Time, 1, false);
                                }
                                catch
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
                                try
                                {
                                    sectors[i] = iRTVO.Overlay.floatTime2String(driver.CurrentLap.SectorTimes.First(s => s.Num.Equals(i)).Time, 1, false);
                                }
                                catch
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
                data.drivers[i] = new webtimingDriver(si);
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
            if (isValidUrl(ref postURL) && SharedData.webError.Length <= 0)
            {
                // Create a request using a URL that can receive a post.
                WebRequest request = WebRequest.Create(postURL);

                // Set the Method property of the request to POST.
                request.Method = "POST";

                // Create POST data and convert it to a byte array.
                byte[] byteArray = Encoding.UTF8.GetBytes("key=" + Properties.Settings.Default.webTimingKey + "&sessionid=" + SharedData.Sessions.SessionId.ToString() + "&subsessionid=" + SharedData.Sessions.SubSessionId.ToString() + "&sessionnum=" + SharedData.Sessions.CurrentSession.Id.ToString() + "&type=" + SharedData.Sessions.CurrentSession.Type.ToString() + "&data=" + postData);

                // Set the ContentType property of the WebRequest.
                request.ContentType = "application/x-www-form-urlencoded";

                // Set the ContentLength property of the WebRequest.
                request.ContentLength = byteArray.Length;

                // Get the request stream.
                Stream dataStream = request.GetRequestStream();

                // Write the data to the request stream.
                dataStream.Write(byteArray, 0, byteArray.Length);

                SharedData.webBytes += byteArray.Length;

                // Close the Stream object.
                dataStream.Close();

                // Get the response.
                WebResponse response;
                try
                {
                    response = request.GetResponse();
                }
                catch (WebException ex)
                {
                    SharedData.webError += "\n" + ex.Message;
                    response = ex.Response as HttpWebResponse;
                }

                // Display the status.
                //Console.WriteLine(((HttpWebResponse)response).StatusDescription);

                // Get the stream containing content returned by the server.
                try
                {
                    dataStream = response.GetResponseStream();
                }
                catch
                {
                    SharedData.webError = "Timeout";
                }

                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);

                // Read the content.
                string responseFromServer = reader.ReadToEnd();

                // Display the content.
                //Console.WriteLine(responseFromServer);
                if (responseFromServer.Length > 0)
                {
                    SharedData.webError += "\n" + responseFromServer;
                    Console.WriteLine("Error posting: " + responseFromServer);
                }

                // Clean up the streams.
                reader.Close();
                dataStream.Close();
                response.Close();

                if(SharedData.webError.Length > 0)
                    System.Windows.MessageBox.Show(SharedData.webError);
            }
           
        }

    }
}
