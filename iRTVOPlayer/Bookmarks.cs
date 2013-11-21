using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVOPlayer
{
    public enum BookmarkEventType
    {
        Start = 0,
        Play,
        Stop
    }

    [Serializable]
    public class BookmarkEvent
    {


        public TimeSpan Timestamp { get; set; }
        public Int64 ReplayPos { get; set; }
        public Int32 Rewind { get; set; }
        public Int32 CamIdx { get; set; }
        public Int32 DriverIdx { get; set; }
        public Int32 PlaySpeed { get; set; }
        public String Description { get; set; }
        public String DriverName { get; set; }
        public Int32 SessionNum { get; set; }

        public BookmarkEventType BookmarkType { get; set; }

        public BookmarkEvent()
        {
        }

    }

    public class Bookmarks
    {
        List<BookmarkEvent> list;

        public Bookmarks()
        {
            list = new List<BookmarkEvent>();
        }

        public List<BookmarkEvent> List { get { return this.list; } set { this.list = value;  } }        
    }
}
