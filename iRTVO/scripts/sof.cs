using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Script : iRTVO.IScript
{
    public iRTVO.IHost Parent { set; get; }

    private int drivercount;
    private double sof;

    public String init()
    {
        this.sof = 0.0;
        this.drivercount = 0;
        return "sof";
    }

    public String DriverInfo(String method, iRTVO.Sessions.SessionInfo.StandingsItem standing, iRTVO.Sessions.SessionInfo session, Int32 rounding)
    {
        switch (method)
        {
            default:
                return "[invalid]";
                break;
        }
    }

    public String SessionInfo(String method, iRTVO.Sessions.SessionInfo session, Int32 rounding)
    {
        switch (method)
        {
            case "sof":
                if (session.Standings.Count != drivercount)
                    this.UpdateSOF();
                return Math.Round(this.sof, rounding).ToString();
                break;
            default:
                return "[invalid]";
                break;
        }
    }

    public void ButtonPress(String method)
    {
    }

    private void UpdateSOF() 
    {
        double basesof = 1600 / Math.Log(2);
        double sofexpsum = 0;

        foreach (iRTVO.DriverInfo driver in Parent.getDrivers())
            sofexpsum += Math.Exp(-driver.iRating / basesof);

        this.drivercount = Parent.getDrivers().Count;
        this.sof = basesof * Math.Log(this.drivercount / sofexpsum);
    }
}
