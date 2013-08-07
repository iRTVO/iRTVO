using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Script : iRTVO.IScript
{
    public iRTVO.IHost Parent { set; get; }

    public String init()
    {
        // returns script name and does other initialization
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
}
