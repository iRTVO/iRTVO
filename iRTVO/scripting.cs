using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptLibrary;

namespace iRTVO
{
    public interface IHost
    {
        iRTVO.Sessions.SessionInfo getSession();
    }

    public interface IScript
    {
        IHost Parent { set; }
        String init();
        String DriverInfo(String method, iRTVO.Sessions.SessionInfo.StandingsItem standing, iRTVO.Sessions.SessionInfo session, Int32 rounding);
        String SessionInfo(String method, iRTVO.Sessions.SessionInfo session, Int32 rounding);

    }

    class Scripting : IHost
    {
        Dictionary<String, IScript> scripts = new Dictionary<String, IScript>();

        // interfaces to app
        public void loadScript(String filename)
        {
            IScript sc = CSScript.Evaluator.LoadFile<IScript>(filename);
            sc.Parent = this;
            String scname = sc.init();
            scripts.Add(scname, sc);
        }

        public String[] getScripts()
        {
            return scripts.Keys.ToArray();
        }

        public String getDriverInfo(String script, String method, Sessions.SessionInfo.StandingsItem standing, Sessions.SessionInfo session, Int32 rounding)
        {
            return scripts[script].DriverInfo(method, standing, session, rounding);
        }

        public String getSessionInfo(String script, String method, Sessions.SessionInfo session, Int32 rounding)
        {
            return scripts[script].SessionInfo(method, session, rounding);
        }

        // interfaces to scripts
        iRTVO.Sessions.SessionInfo IHost.getSession()
        {
            return SharedData.Sessions.CurrentSession;
        }
    }
}
