using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iRTVO.Interfaces;
using iRSDKSharp;

public class Script : IScript
{
    public IHost Parent { set; get; }
    private iRacingSDK sdk;

    public ScriptInterfaceRequestType RequestedInterfaces { get { return ScriptInterfaceRequestType.None; } }

    public String init(IHost parent)
    {
        Parent = parent;
        // returns script name and does other initialization
        this.sdk = new iRacingSDK();
        this.sdk.Startup();
        return "helloworld";
    }

    public String DriverInfo(String method, IStandingsItem standing, ISessionInfo session, Int32 rounding)
    {
        switch (method)
        {
            case "test":
                return "test succesful";
                break;
            case "drivername":
                return standing.Driver.Name;
                break;
            case "fuel":
                return ((Single)this.sdk.GetData("FuelLevel")).ToString();
                break;
            default:
                return "[invalid]";
                break;
        }
    }

    public String SessionInfo(String method, ISessionInfo session, Int32 rounding)
    {
        switch (method)
        {
            case "test":
                return "test succesful";
                break;
            case "state":
                return session.State.ToString();
                break;
            default:
                return "[invalid]";
                break;
        }
    }

    public void ButtonPress(String method)
    {
        switch (method)
        {
            case "buttontest":
                if (this.sdk.IsConnected())
                    Console.WriteLine("[helloworld] replay speed is " + this.sdk.GetData("ReplayPlaySpeed"));
                else
                    Console.WriteLine("[helloworld] not connected to API");
                break;
            default:
                Console.WriteLine("[helloworld] Unknown method (" + method + ") called");
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
