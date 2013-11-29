using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptLibrary;
using iRSDKSharp;
using iRTVO.Networking;
using System.Reflection;
using NLog;
using iRTVO.Data;

namespace iRTVO
{
    [Flags]
    public enum InterfaceRequestType : int
    {
        None = 0,
        ApiTick = 1,
        OverlayTick = 2
    }

    public interface IHost
    {
        SessionInfo getSession();
        List<DriverInfo> getDrivers();
        iRTVO.Theme getTheme();
        Settings getSettings();
        TrackInfo getTrackInfo();
        CameraInfo getCameraInfo();
        void SwitchCamera(int camera, int driver);
        Dictionary<int, string[]> getExternalData();
    }

    public interface IScript
    {
        IHost Parent { get; set; }
        String init();

        InterfaceRequestType RequestedInterfaces { get; }

        String DriverInfo(String method, StandingsItem standing, SessionInfo session, Int32 rounding);
        String SessionInfo(String method, SessionInfo session, Int32 rounding);
        void ButtonPress(String method);
        void ApiTick(iRacingSDK api);
        void OverlayTick(iRTVO.Overlay overlay);
    }


    class Scripting : IHost
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        Dictionary<String, IScript> scripts = new Dictionary<String, IScript>();

        public Scripting()
        {
            CSScript.AssemblyResolvingEnabled = true;

        }
        // interfaces to app
        public void loadScript(String filename)
        {
            try
            {
                logger.Trace("Loading script {0}", filename);
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
            catch (Exception ex)
            {
                logger.Error("Error loading script {0}: {1}", filename, ex.ToString());
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

        public String getDriverInfo(String script, String method, StandingsItem standing, SessionInfo session, Int32 rounding)
        {
            try {
                string result = scripts[script].DriverInfo(method, standing, session, rounding);
                logger.Debug("Calling getDriverInfo('{0}') in {1} Result: {2}", method, script, result);
                return result;
            }
            catch (Exception ex)
            {
                logger.Error("Error in {0}.DriverInfo('{2}'): {1}", script, ex.ToString(), method);
            }
            return "[Error]";
        }

        public String getSessionInfo(String script, String method, SessionInfo session, Int32 rounding)
        {
           try {
               string result = scripts[script].SessionInfo(method, session, rounding);
               logger.Debug("Calling getSessionInfo('{0}') in {1} Result: {2}", method, script, result);
               return result;
           }
           catch (Exception ex)
           {
               logger.Error("Error in {0}.SessionInfo('{2}'): {1}",script, ex.ToString(),method);
           }
           return "[Error]";
        }

        public void PressButton(String script, String method)
        {
            try
            {
                logger.Debug("Calling Pressbutton {0} in {1}", method, script);
                scripts[script].ButtonPress(method);
            }
            catch (Exception ex)
            {
                logger.Error("Error in {0}.PressButton: {1}", script, ex.ToString());
            }
        }

        public void ApiTick(iRacingSDK api)
        {
            foreach (var pair in this.scripts)
            {
                try
                {
                    if (!pair.Value.RequestedInterfaces.HasFlag(InterfaceRequestType.ApiTick) )
                        continue;
                    logger.Debug("Calling ApiTick in {0}", pair.Key);
                    using (new TimeCall("ApiTick")) 
                        pair.Value.ApiTick(api);
                }
                catch (Exception ex)
                {
                    logger.Error("Error in {0}.ApiTick: {1}", pair.Key, ex.ToString());
                }
            }
        }

        public void OverlayTick(iRTVO.Overlay overlay)
        {
            foreach (var pair in this.scripts)
            {
                try {
                    if (!pair.Value.RequestedInterfaces.HasFlag(InterfaceRequestType.OverlayTick))
                        continue;
                    logger.Debug("Calling OverlayTick in {0}", pair.Key);
                    using (new TimeCall("ApiTick"))
                        pair.Value.OverlayTick(overlay);
                }
                catch (Exception ex)
                {
                    logger.Error("Error in {0}.OverlayTick: {1}", pair.Key, ex.ToString());
                }
            }
        }

        // interfaces to scripts
        SessionInfo IHost.getSession()
        {
            return SharedData.Sessions.CurrentSession;
        }

        List<DriverInfo> IHost.getDrivers()
        {
            return SharedData.Drivers;
        }

        iRTVO.Theme IHost.getTheme()
        {
            return SharedData.theme;
        }

        Settings IHost.getSettings()
        {
            return SharedData.settings;
        }

        TrackInfo IHost.getTrackInfo()
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
