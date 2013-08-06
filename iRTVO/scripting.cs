using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptLibrary;

namespace iRTVO
{
    public interface IHost
    {
        void Who();
        List<iRTVO.Sessions.SessionInfo.StandingsItem> getStandings();
    }

    public interface IScript
    {
        IHost Parent { set; }
        String init();
    }

    class Scripting : IHost
    {
        Dictionary<String, IScript> scripts = new Dictionary<String, IScript>();

        public void loadScript(String filename)
        {
            IScript sc = CSScript.Evaluator.LoadFile<IScript>(filename);
            sc.Parent = this;
            String scname = sc.init();
            scripts.Add(scname, sc);
        }

        void IHost.Who()
        {
            Console.WriteLine("following " + SharedData.Sessions.CurrentSession.FollowedDriver.Driver.Name);
        }

        List<iRTVO.Sessions.SessionInfo.StandingsItem> IHost.getStandings()
        {
            return SharedData.Sessions.CurrentSession.Standings;
        }
    }
}
