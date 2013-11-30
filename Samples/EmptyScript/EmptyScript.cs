using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iRTVO.Interfaces;


public class Script : IScript
{
    private IHost Parent { set; get; }

    public ScriptInterfaceRequestType RequestedInterfaces { get { return ScriptInterfaceRequestType.None } }

    public String init(IHost parent)
    {
        Parent = parent;
        // returns script name and does other initialization        
        return "EmptyScript";
    }

    public String DriverInfo(String method, IStandingsItem standing, ISessionInfo session, Int32 rounding)
    {
        switch (method)
        {            
            default:
                return "[invalid]";
                break;
        }
    }

    public String SessionInfo(String method, ISessionInfo session, Int32 rounding)
    {
        switch (method)
        {            
            default:
                return "[invalid]";
                break;
        }
    }

    public void ButtonPress(String method)
    {
        switch (method)
        {           
            default:
                Console.WriteLine("[EmptyScript] Unknown method (" + method + ") called");
                break;
        }
    }

    public void ApiTick(ISimulationAPI api)
    {
    }

    public void OverlayTick()
    {
    }
}
