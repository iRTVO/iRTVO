using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptLibrary;
using iRSDKSharp;
using iRTVO.Networking;
using System.Reflection;

namespace iRTVO
{
    public interface IHost
    {
        iRTVO.Sessions.SessionInfo getSession();
        List<iRTVO.DriverInfo> getDrivers();
        iRTVO.Theme getTheme();
        iRTVO.Settings getSettings();
        iRTVO.TrackInfo getTrackInfo();
        CameraInfo getCameraInfo();
        void SwitchCamera(int camera, int driver);
        Dictionary<int, string[]> getExternalData();
    }

    public interface IScriptBase
    {
        IHost Parent { get; set; }
        String init();
        String DriverInfo(String method, iRTVO.Sessions.SessionInfo.StandingsItem standing, iRTVO.Sessions.SessionInfo session, Int32 rounding);
        String SessionInfo(String method, iRTVO.Sessions.SessionInfo session, Int32 rounding);
        void ButtonPress(String method);
    }

    public interface IScript : IScriptBase
    {
        void ApiTick(iRacingSDK api);
        void OverlayTick(iRTVO.Overlay overlay);
    }

    class Scripting : IHost
    {
        Dictionary<String, IScript> scripts = new Dictionary<String, IScript>();

        public Scripting()
        {
            CSScript.AssemblyResolvingEnabled = true;

        }
        // interfaces to app
        public void loadScript(String filename)
        {
            Assembly script = CSScript.Load(filename, null, true);
            foreach (var t in script.GetTypes())
            {
                Type tp = t.GetInterface("IScript");
                if (tp != null)
                {
                    IScript sc = Activator.CreateInstance(t) as IScript;
            sc.Parent = this;
            String scname = sc.init();
            scripts.Add(scname, sc);
        }
            }
        }

        // Allow adding of precompiled scripts
        public void addScript(IScript sc)
        {
            sc.Parent = this;
            String scname = sc.init();
            scripts.Add(scname, sc);
        }

        public String[] Scripts { get { return scripts.Keys.ToArray(); } set { } }

        public String getDriverInfo(String script, String method, Sessions.SessionInfo.StandingsItem standing, Sessions.SessionInfo session, Int32 rounding)
        {
            return scripts[script].DriverInfo(method, standing, session, rounding);
        }

        public String getSessionInfo(String script, String method, Sessions.SessionInfo session, Int32 rounding)
        {
            return scripts[script].SessionInfo(method, session, rounding);
        }

        public void PressButton(String script, String method)
        {
            scripts[script].ButtonPress(method);
        }

        public void ApiTick(iRacingSDK api)
        {
            foreach (var pair in this.scripts)
                pair.Value.ApiTick(api);
        }

        public void OverlayTick(iRTVO.Overlay overlay)
        {
            foreach (var pair in this.scripts)
                pair.Value.OverlayTick(overlay);
        }

        // interfaces to scripts
        iRTVO.Sessions.SessionInfo IHost.getSession()
        {
            return SharedData.Sessions.CurrentSession;
        }

        List<iRTVO.DriverInfo> IHost.getDrivers()
        {
            return SharedData.Drivers;
        }

        iRTVO.Theme IHost.getTheme()
        {
            return SharedData.theme;
        }

        iRTVO.Settings IHost.getSettings()
        {
            return SharedData.settings;
        }

        iRTVO.TrackInfo IHost.getTrackInfo()
        {
            return SharedData.Track;
        }

        public CameraInfo getCameraInfo()
        {
            return SharedData.Camera;
        }

        public void SwitchCamera(int camera, int driver)
        {
            iRTVOConnection.BroadcastMessage("CAMERA" , camera);
            iRTVOConnection.BroadcastMessage("DRIVER" , driver);
        }


        public Dictionary<int, string[]> getExternalData()
            {
            return SharedData.externalData;
        }
    }
}
