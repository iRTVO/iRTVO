using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Interfaces
{

    public interface IHost
    {
        ISessionInfo getSession();
        IList<IDriverInfo> getDrivers();
        // Theme getTheme();
        ISettings getSettings();
        ITrackInfo getTrackInfo();
        ICameraInfo getCameraInfo();
        
        Dictionary<int, string[]> getExternalData();

        void SwitchCamera(int camera, int driver);
        void UpdateExternalData();
    }


    public interface IScript
    {         
        String init(IHost Parent);

        ScriptInterfaceRequestType RequestedInterfaces { get; }

        String DriverInfo(String method, IStandingsItem standing, ISessionInfo session, Int32 rounding);
        String SessionInfo(String method, ISessionInfo session, Int32 rounding);
        void ButtonPress(String method);
        void ApiTick(ISimulationAPI api);

        void OverlayTick(); // NO Interface for iRTVO.Overlay overlay yet. Is this needed anyway?!
    }
}
