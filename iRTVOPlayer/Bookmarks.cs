using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVOPlayer
{
    public enum BookmarkType
    {
        Start = 0,
        Play,
        Stop
    }

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
        public Int32 SessionNum { get; set; }

        public BookmarkType BookmarkType { get; set; }

        public Bookmark()
        {
        }

    }

    public class Bookmarks
    {
        List<Bookmark> list;

        public Bookmarks()
        {
            list = new List<Bookmark>();
        }

        public List<Bookmark> List { get { return this.list; } set { this.list = value;  } }        
    }
}
