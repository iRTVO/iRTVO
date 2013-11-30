using System;

using iRTVO.Interfaces;

public class Script : IScript
{
    /* CONFIGURATION */
    private Int32 length = 6*60; // frames (1/60s)
    private Int32 max_value = 9;

    private Int32 state;
    private Int32 position;

    private Int32 ease(Int32 x)
    {
        return (Int32)((Double)this.max_value * Math.Pow(2f, 10f * (((Double)x / (Double)this.length) - 1f)) + 1f);
    }

    public IHost Parent { set; get; }
   
    public ScriptInterfaceRequestType RequestedInterfaces { get { return ScriptInterfaceRequestType.ApiTick; } }

    public String init(IHost parent)
    {
        // returns script name and does other initialization
        this.Parent = parent;
        this.state = 0;
        this.position = this.length;
        return "slowmo";
    }

    public void ButtonPress(String method)
    {
        switch (method)
        {
            case "slowmo":
                state = -1;
                position = 0;
                break;
            case "normal":
                state = 1;
                position = 0;
                break;
            default:
                break;
        }
    }

    public void ApiTick(ISimulationAPI api)
    {
        if (this.position < length)
        {
            switch (this.state)
            {
                case 1:
                    api.ReplaySetPlaySpeed(this.max_value - ease(this.position) + 1, 1);
                    break;
                case -1:
                    api.ReplaySetPlaySpeed( ease(this.position), 1);
                    break;
                default:
                    break;
            }
        }
        else if (this.position >= length+10 && this.state != 0)
        {
            Console.WriteLine("Finished..." + this.state);
            if(this.state == 1)
                api.ReplaySetPlaySpeed( 1, 0);
            else if(this.state == -1)
                api.ReplaySetPlaySpeed( this.max_value, 1);
            this.state = 0; 
        }

        if(this.state != 0)
            position++;
    }

    public void OverlayTick()
    {
    }

    public String DriverInfo(String method, IStandingsItem standing, ISessionInfo session, Int32 rounding)
    {
        return "";
    }

    public String SessionInfo(String method, ISessionInfo session, Int32 rounding)
    {
        return "";
    }

}
