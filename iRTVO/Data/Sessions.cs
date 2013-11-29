using iRTVO.Interfaces;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class Sessions : INotifyPropertyChanged, ISessions
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        List<SessionInfo> sessions;
        int currentsession;
        int sessionid;
        int subsessionid;
        bool hosted;

        public Sessions()
        {
            sessions = new List<SessionInfo>();
            currentsession = 0;
            sessionid = 0;
            subsessionid = 0;
            hosted = false;
        }

        public List<SessionInfo> SessionList { get { return sessions; } set { sessions = value; } }
        public SessionInfo CurrentSession { get { if (sessions.Count > 0) return sessions[currentsession]; else return new SessionInfo(); } set { } }
        public int SessionId { get { return sessionid; } set { sessionid = value; } }
        public int SubSessionId { get { return subsessionid; } set { subsessionid = value; } }
        public bool Hosted { get { return hosted; } set { hosted = value; } }

        public void setCurrentSession(int id)
        {
            int index = sessions.FindIndex(s => s.Id.Equals(id));
            if (index >= 0)
            {
                if (currentsession != index)
                {
                    currentsession = index;
                    this.NotifyPropertyChanged("CurrentSession");
                }
            }
            else
            {
                currentsession = 0;
                this.NotifyPropertyChanged("CurrentSession");
            }

        }

        public SessionInfo findSessionType(SessionTypes type)
        {
            int index = sessions.FindIndex(s => s.Type.Equals(type));
            if (index >= 0)
            {
                return SessionList[index];
            }
            else
            {
                return new SessionInfo();
            }
        }

        ISessionInfo ISessions.CurrentSession
        {
            get { return CurrentSession; }
        }

        ISessionInfo ISessions.findSessionType(SessionTypes type)
        {
            return findSessionType(type);
        }
        

        IList<ISessionInfo> ISessions.SessionList
        {
            get { return SessionList as IList<ISessionInfo>; }
        }
        
    }

}
