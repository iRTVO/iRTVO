using iRTVO.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class Sector : iRTVO.Interfaces.ISector
    {
        Int32 num;
        Single time;
        Single speed;
        Double begin;

        public Sector()
        {
            num = 0;
            time = 0;
            speed = 0;
            begin = 0;
        }

        public Int32 Num { get { return num; } set { num = value; } }
        public Single Time { get { return time; } set { time = value; } }
        public Single Speed { get { return speed; } set { speed = value; } }
        public Double Begin { get { return begin; } set { begin = value; } }
    }

    public class LapInfo : INotifyPropertyChanged, iRTVO.Interfaces.ILapInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }       
        Int32 lapnum;
        Single laptime;
        Int32 position;
        Single gap;
        Int32 gaplaps;
        List<Sector> sectortimes;
        Int32 replayPos;
        Double sessionTime;
        Int32 classposition;

        public LapInfo()
        {
            lapnum = 0;
            laptime = 0;
            position = 0;
            classposition = 0;
            gap = 0;
            gaplaps = 0;
            sectortimes = new List<Sector>(3);
            replayPos = 0;
        }

        public Int32 LapNum { get { return lapnum; } set { lapnum = value; } }
        public Single LapTime { get { if (laptime == float.MaxValue) return 0.0f; else { return laptime; } } set { laptime = value; } }
        public string LapTime_HR { get { if (laptime != float.MaxValue) return Utils.floatTime2String(laptime, 3, false); else return String.Empty; } }
        public Int32 Position { get { return position; } set { position = value; } }
        public Int32 ClassPosition { get { return classposition; } set { classposition = value; } }
        public Single Gap { get { if (gap == float.MaxValue) return 0; else { return gap; } } set { gap = value; } }
        public Int32 GapLaps { get { return gaplaps; } set { gaplaps = value; } }
        public Int32 ReplayPos { get { return replayPos; } set { replayPos = value; } }
        public Double SessionTime { get { return sessionTime; } set { sessionTime = value; } }
        public List<Sector> SectorTimes { get { return sectortimes; } set { sectortimes = value; } }

        // combined Gap and GapLaps
        public string Gap_HR
        {
            get
            {
                if (gaplaps > 0)
                    return gaplaps + " L";
                else if (gap == float.MaxValue)
                    return "-.--";
                else
                    return gap.ToString("0.000");
            }
            set { }
        }

        IList<ISector> ILapInfo.SectorTimes
        {
            get { return sectortimes as IList<ISector>; }
        }

        
    }
}
