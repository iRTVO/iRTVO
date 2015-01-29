using iRTVO.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace iRTVO.Data
{   
    [Serializable]
    public class Bookmark
    {
        public TimeSpan Timestamp { get; set; }
        public Int64 ReplayPos { get; set; }
        public Int32 Rewind { get; set; }
        public Int32 CamIdx { get; set; }
        public Int32 DriverIdx { get; set; }
        public Int32 PlaySpeed { get; set; }
        public String Description { get; set; }
        public String DriverName { get; set; }
        public Int32 SessionNum { get; set; }       // KJ: needed for rewritten "REWIND" broadcast

        public BookmarkTypes BookmarkType { get; set; }

        public Bookmark()
        {
        }

        public Bookmark(SessionEvent ev)
        {
            ReplayPos = ev.ReplayPos;
            Rewind = ev.Rewind;
            CamIdx = SharedData.currentCam;
            DriverIdx = ev.Driver.NumberPlatePadded;
            PlaySpeed = 0;
            SessionNum = ev.SessionNumber;      // KJ: needed for rewritten "REWIND" broadcast
        }
    }

    public class Bookmarks : INotifyPropertyChanged
    {
        ObservableCollection<Bookmark> list;

        public Bookmarks()
        {
            list = new ObservableCollection<Bookmark>();
        }

        public ObservableCollection<Bookmark> List { get { return this.list; } set { this.list = value; this.NotifyPropertyChanged("List"); } }
        public int SessionID { get; set; }
        public int SubSessionID { get; set; }

        [XmlIgnore]
        public Int64 MaxReplayPos
        {
            get { return this.list.Max(r => r.ReplayPos); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
