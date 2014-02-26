using System;
using System.Collections.Generic;
using System.Text;
using iRTVO.Interfaces;

public class Script : IScript
{
    public IHost Parent { set; get; }

    private Int32 drivercount;
    private Double sof;
    private List<Double> points;

    public ScriptInterfaceRequestType RequestedInterfaces { get { return ScriptInterfaceRequestType.None; } }

    public String init(IHost parent)
    {
        Parent = parent;
        this.sof = 0.0;
        this.drivercount = 0;
        this.points = new List<Double>();
        return "sof";
    }

    public String DriverInfo(String method, IStandingsItem standing, ISessionInfo session, Int32 rounding)
    {
        switch (method)
        {
            case "officialpoints":
                if (session.Standings.Count != drivercount)
                    this.UpdateSOF();
                return Math.Round(this.points[standing.Position], MidpointRounding.ToEven).ToString();
            default:
                return "[invalid]";
        }
    }

    public String SessionInfo(String method, ISessionInfo session, Int32 rounding)
    {
        switch (method)
        {
            case "sof":
                if (session.Standings.Count != drivercount)
                    this.UpdateSOF();
                return Math.Round(Math.Floor(this.sof), rounding).ToString();
            default:
                return "[invalid]";
        }
    }


    public void ButtonPress(String method)
    {
    }

    public void ApiTick(ISimulationAPI api)
    {
    }

    public void OverlayTick()
    {
    }

    private void UpdateSOF()
    {
		if (Parent == null )
			{
			this.sof = -1;
				return;
				}
		 IList<IDriverInfo> dr = Parent.getDrivers();
		 if ( dr == null )
		 	{
			this.sof = -2;
				return;
				}
		 
        this.drivercount = dr.Count;
        this.points = new List<Double>();

        // sof
        double basesof = 1600 / Math.Log(2);
        double sofexpsum = 0;

        foreach (IDriverInfo driver in Parent.getDrivers())
            sofexpsum += Math.Exp(-driver.iRating / basesof);

        this.sof = basesof * Math.Log(this.drivercount / sofexpsum);

        // points
        double winnerpoints;

        winnerpoints = this.sof / 16 * 1.06 * this.drivercount / (this.drivercount + 1);
        for (int i = 0; i < this.drivercount; i++)
            points.Add(winnerpoints * (this.drivercount - i) / (this.drivercount - 1));
        // last position is half of the second last
        points.Add(points[points.Count-1] / 2);

    }
}
