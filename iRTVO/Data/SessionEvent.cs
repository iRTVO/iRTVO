using iRTVO.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class SessionEvent : INotifyPropertyChanged, ISessionEvent
    {        
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        SessionEventTypes type;
        DateTime timestamp;
        Int64 replaypos;
        String description;
        DriverInfo driver;
        SessionTypes session;
        Int32 lapnum;
        Int32 rewind;

        public SessionEvent()
        {

        }

        public SessionEvent(SessionEventTypes type, Int64 replay, DriverInfo driver, String desc, SessionTypes session, Int32 lap)
        {
            this.type = type;
            this.timestamp = DateTime.Now;
            this.replaypos = replay;
            this.driver = driver;
            this.description = desc;
            this.session = session;
            this.lapnum = lap;
            this.rewind = 0;
        }

        public String Session { get { return this.session.ToString(); } set { } }
        public DateTime Timestamp { get { return this.timestamp; } set { } }
        public Int64 ReplayPos { get { return this.replaypos; } set { } }
        public String Description { get { return this.description; } set { } }
        public DriverInfo Driver { get { return this.driver; } set { } }
        public SessionEventTypes EventType { get { return this.type; } set { } }
        public Int32 Lap { get { return this.lapnum; } set { } }
        public Int32 Rewind { get { return this.rewind; } set { this.rewind = value; } }


        IDriverInfo ISessionEvent.Driver { get { return Driver as IDriverInfo; } }
    }

    
}
