using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class TrackInfo : iRTVO.Interfaces.ITrackInfo
    {
        private Int32 id;

        public Int32 Id
        {
            get { return id; }
            set { id = value; }
        }
        private Single length;

        public Single Length
        {
            get { return length; }
            set { length = value; }
        }
        private Int32 turns;

        public Int32 Turns
        {
            get { return turns; }
            set { turns = value; }
        }
        private String name = "";

        public String Name
        {
            get { return name; }
            set { name = value; }
        }

        private String city = "";

        public String City
        {
            get { return city; }
            set { city = value; }
        }
        private String country = "";

        public String Country
        {
            get { return country; }
            set { country = value; }
        }
        private Single altitude;

        public Single Altitude
        {
            get { return altitude; }
            set { altitude = value; }
        }

        private String sky = "Clear";

        public String Sky
        {
            get { return sky; }
            set { sky = value; }
        }
        private Single tracktemp;

        public Single TrackTemperature
        {
            get { return tracktemp; }
            set { tracktemp = value; }
        }
        private Single airtemp;

        public Single AirTemperature
        {
            get { return airtemp; }
            set { airtemp = value; }
        }
        private Int32 humidity;

        public Int32 Humidity
        {
            get { return humidity; }
            set { humidity = value; }
        }
        private Int32 fog;

        public Int32 Fog
        {
            get { return fog; }
            set { fog = value; }
        }

        private Single airpressure;

        public Single AirPressure
        {
            get { return airpressure; }
            set { airpressure = value; }
        }
        private Single windspeed;

        public Single WindSpeed
        {
            get { return windspeed; }
            set { windspeed = value; }
        }
        private Single winddirection;

        public Single WindDirection
        {
            get { return winddirection; }
            set { winddirection = value; }
        }

    }
}
