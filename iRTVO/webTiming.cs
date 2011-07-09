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

            public webtimingDriver(StandingsItem driver)
            {
                position = driver.Position.ToString();
                name = driver.Driver.Name;
                number = driver.Driver.NumberPlate;
                lap = driver.CurrentLap.LapNum.ToString();
                fastestlap = iRTVO.Overlay.floatTime2String(driver.FastestLap, true, false);
                previouslap = iRTVO.Overlay.floatTime2String(driver.PreviousLap.LapTime, true, false);
                interval = driver.IntervalLive_HR;
                gap = driver.GapLive_HR;
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


            public webtimingDriver[] drivers;
        }
        
        private string postURL;

        public webTiming(string url) {
            postURL = url;

        }

        public void postData(object o)
        {
            webTimingObject data = new webTimingObject();

            data.trackname = SharedData.Track.name;
            data.sessiontype = SharedData.Sessions.CurrentSession.Type.ToString();
            data.sessionstate = SharedData.Sessions.CurrentSession.State.ToString();
            data.sessionflag = SharedData.Sessions.CurrentSession.Flag.ToString();
            data.currentlap = SharedData.Sessions.CurrentSession.LapsComplete;
            data.totallaps = SharedData.Sessions.CurrentSession.LapsTotal;
            data.timeremaining = (float)SharedData.Sessions.CurrentSession.TimeRemaining;

            data.drivers = new webtimingDriver[SharedData.Sessions.CurrentSession.Standings.Count];

            IEnumerable<StandingsItem> query = SharedData.Sessions.CurrentSession.Standings.OrderBy(s => s.Position);

            int i = 0;
            foreach (StandingsItem si in query)
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
                byte[] byteArray = Encoding.UTF8.GetBytes("key=" + Properties.Settings.Default.webTimingKey + "&sessionid=" + SharedData.Sessions.SessionId.ToString() + "&subsessionid=" + SharedData.Sessions.SubSessionId.ToString() + "&sessionnum=" + SharedData.Sessions.CurrentSession.Id.ToString() + "&data=" + postData);

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
                dataStream = response.GetResponseStream();

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

                Console.WriteLine(DateTime.Now.ToString() + " web updated");

                if(SharedData.webError.Length > 0)
                    System.Windows.MessageBox.Show(SharedData.webError);
            }
           
        }

    }
}
