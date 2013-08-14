using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iRSDKSharp;

public class Script : iRTVO.IScript
{
    public iRTVO.IHost Parent { set; get; }

    private iRacingSDK sdk;

    public String init()
    {
        // returns script name and does other initialization
        this.sdk = new iRacingSDK();
        this.sdk.Startup();
        return "helloworld";
    }

    public String DriverInfo(String method, iRTVO.Sessions.SessionInfo.StandingsItem standing, iRTVO.Sessions.SessionInfo session, Int32 rounding)
    {
        switch (method)
        {
            case "test":
                return "test succesful";
                break;
            case "drivername":
                return standing.Driver.Name;
                break;
            default:
                return "[invalid]";
                break;
        }
    }

    public String SessionInfo(String method, iRTVO.Sessions.SessionInfo session, Int32 rounding)
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
}
