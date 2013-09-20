using System;
using System.Collections.Generic;
using System.Text;
using iRSDKSharp;

public class Script : iRTVO.IScript
{
    public iRTVO.IHost Parent { set; get; }

    private Int32 drivercount;
    private Double sof;
    private List<Double> points;

    public String init()
    {
        this.sof = 0.0;
        this.drivercount = 0;
        this.points = new List<Double>();
        return "sof";
    }

    public String DriverInfo(String method, iRTVO.Sessions.SessionInfo.StandingsItem standing, iRTVO.Sessions.SessionInfo session, Int32 rounding)
    {
        switch (method)
        {
            case "officialpoints":
                if (session.Standings.Count != drivercount)
                    this.UpdateSOF();
                return Math.Round(this.points[standing.Position], MidpointRounding.ToEven).ToString();
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
            case "sof":
                if (session.Standings.Count != drivercount)
                    this.UpdateSOF();
                return Math.Round(Math.Floor(this.sof), rounding).ToString();
                break;
            default:
                return "[invalid]";
                break;
        }
    }


    public void ButtonPress(String method)
    {
    }

    public void ApiTick(iRacingSDK api)
    {
    }

    public void OverlayTick(iRTVO.Overlay overlay)
    {
    }

    private void UpdateSOF()
    {
        this.drivercount = Parent.getDrivers().Count;
        this.points = new List<Double>();

        // sof
        double basesof = 1600 / Math.Log(2);
        double sofexpsum = 0;

        foreach (iRTVO.DriverInfo driver in Parent.getDrivers())
            sofexpsum += Math.Exp(-driver.iRating / basesof);

        this.sof = basesof * Math.Log(this.drivercount / sofexpsum);

        // points
        double winnerpoints;

        winnerpoints = this.sof / 16 * 1.06 * this.drivercount / (this.drivercount + 1);
        for (int i = 0; i < this.drivercount; i++)
            points.Add(winnerpoints * (this.drivercount - i) / (this.drivercount - 1));
        // last position is half of the second last
        points.Add(points.Last() / 2);

    }
}
