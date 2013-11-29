using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class TrackInfo
    {
        public Int32 id;
        public Single length;
        public Int32 turns;
        public String name = "";

        public String city = "";
        public String country = "";
        public Single altitude;

        public String sky = "Clear";
        public Single tracktemp;
        public Single airtemp;
        public Int32 humidity;
        public Int32 fog;

        public Single airpressure;
        public Single windspeed;
        public Single winddirection;

    }
}
