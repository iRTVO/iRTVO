using System;
using iRSDKSharp;

public class Script : iRTVO.IScript
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

    public iRTVO.IHost Parent { set; get; }
    private iRacingSDK sdk;

    public String init()
    {
        // returns script name and does other initialization
        this.sdk = new iRacingSDK();
        this.sdk.Startup();
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

    public void ApiTick(iRacingSDK api)
    {
        if (this.position < length)
        {
            switch (this.state)
            {
                case 1:
                    api.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, this.max_value - ease(this.position) + 1, 1);
                    break;
                case -1:
                    api.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, ease(this.position), 1);
                    break;
                default:
                    break;
            }
        }
        else if (this.position >= length+10 && this.state != 0)
        {
            Console.WriteLine("Finished..." + this.state);
            if(this.state == 1)
                api.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
            else if(this.state == -1)
                api.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, this.max_value, 1);
            this.state = 0; 
        }

        if(this.state != 0)
            position++;
    }

    public void OverlayTick(iRTVO.Overlay overlay)
    {
    }

    public String DriverInfo(String method, iRTVO.Sessions.SessionInfo.StandingsItem standing, iRTVO.Sessions.SessionInfo session, Int32 rounding)
    {
        return "";
    }

    public String SessionInfo(String method, iRTVO.Sessions.SessionInfo session, Int32 rounding)
    {
        return "";
    }

}
