using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class CameraInfo : INotifyPropertyChanged
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public class CameraGroup
        {

            string name;
            int id;

            public CameraGroup()
            {
                name = "";
                id = -1;
            }

            public string Name { get { return name; } set { name = value; } }
            public int Id { get { return id; } set { id = value; } }
        }

        public CameraGroup FindId(int id)
        {
            int index = groups.IndexOf(groups.Where(g => g.Id.Equals(id)).FirstOrDefault());
            if (index >= 0)
            {
                return groups[index];
            }
            else
            {
                return new CameraGroup();
            }
        }

        int currentgroup;
        int wantedgroup;
        ObservableCollection<CameraGroup> groups;
        DateTime updated;

        public CameraInfo()
        {
            currentgroup = 0;
            wantedgroup = 0;
            groups = new ObservableCollection<CameraGroup>();
            updated = DateTime.Now;
        }

        public int CurrentGroup
        {
            get { return currentgroup; }
            set
            {
                if (currentgroup != value)
                {
                    logger.Trace("SimCamChange Currentgroup old={0} new={1}", currentgroup, value);
                    currentgroup = value;
                    NotifyPropertyChanged("CurrentGroup");
                }
            }
        }
        public int WantedGroup { get { return wantedgroup; } set { wantedgroup = value; } }
        public ObservableCollection<CameraGroup> Groups { get { return groups; } set { groups = value; updated = DateTime.Now; NotifyPropertyChanged("Groups"); } }
        public DateTime Updated { get { return updated; } set { } }


        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}
