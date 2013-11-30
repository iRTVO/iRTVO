using iRTVO.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVOCompiler
{
    public class Host : IHost
    {
        public ISessionInfo getSession()
        {
            throw new NotImplementedException();
        }

        public IList<IDriverInfo> getDrivers()
        {
            throw new NotImplementedException();
        }

        public ISettings getSettings()
        {
            throw new NotImplementedException();
        }

        public ITrackInfo getTrackInfo()
        {
            throw new NotImplementedException();
        }

        public ICameraInfo getCameraInfo()
        {
            throw new NotImplementedException();
        }

        public Dictionary<int, string[]> getExternalData()
        {
            throw new NotImplementedException();
        }

        public void SwitchCamera(int camera, int driver)
        {
            throw new NotImplementedException();
        }
    }
}
