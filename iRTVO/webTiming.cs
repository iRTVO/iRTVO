using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// additional
using Newtonsoft.Json;
using System.IO;
using System.Net;

namespace iRTVO
{
    class webTiming
    {        
        
        private string postURL;

        public webTiming(string url) {
            postURL = url;
        }

        public void postStanding()
        {
            send("standing", JsonConvert.SerializeObject(SharedData.standing));
        }

        public void postDrivers()
        {
            send("drivers", JsonConvert.SerializeObject(SharedData.drivers));
        }

        public void postSessions()
        {
            send("sessions", JsonConvert.SerializeObject(SharedData.sessions));
        }

        public void send(string type, string postData)
        {
            // Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create(postURL);

            // Set the Method property of the request to POST.
            request.Method = "POST";

            // Create POST data and convert it to a byte array.
            byte[] byteArray = Encoding.UTF8.GetBytes(type + "=" + postData);

            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/x-www-form-urlencoded";

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;

            // Get the request stream.
            Stream dataStream = request.GetRequestStream();

            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);

            // Close the Stream object.
            dataStream.Close();

            // Get the response.
            WebResponse response = request.GetResponse();

            // Display the status.
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();

            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);

            // Read the content.
            string responseFromServer = reader.ReadToEnd();

            // Display the content.
            Console.WriteLine(responseFromServer);

            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();
        }

    }
}
