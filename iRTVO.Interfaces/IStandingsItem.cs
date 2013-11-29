using System;
using System.Collections.Generic;
using System.ComponentModel;
namespace iRTVO.Interfaces
{
    public interface IStandingsItem
    {
        TimeSpan AirTimeAirTime { get;  }
        string AirTimeAirTime_HR { get; }
        int AirTimeCount { get; }
        DateTime AirTimeLastAirTime { get; }
        double Begin { get;  }
        string ClassGapLive_HR { get;  }
        string ClassIntervalLive_HR { get;  }
        int ClassLapsLed { get;  }
        ILapInfo CurrentLap { get;  }
        double CurrentTrackPct { get;  }
        double DistanceToFollowed { get;  }
        IDriverInfo Driver { get;  }
        float FastestLap { get;  }
        string FastestLap_HR { get; }
        ILapInfo FindLap(int num);
        bool Finished { get;  }
        double GapLive { get;  }
        string GapLive_HR(int rounding);
        string GapLive_HR_rounded { get; }
        int HighestClassPosition { get;  }
        int HighestPosition { get;  }
        double IntervalLive { get;  }
        string IntervalLive_HR(int rounding);
        string IntervalLive_HR_rounded { get;  }
        double IntervalToFollowedLive { get;  }
        bool IsFollowedDriver { get;  }
        IList<ILapInfo> Laps { get;  }
        int LapsLed { get;  }
        int LowestClassPosition { get;  }
        int LowestPosition { get;  }
        double OffTrackSince { get;  }
        DateTime PitStopBegin { get;  }
        int PitStops { get;  }
        float PitStopTime { get;  }
        int Position { get;  }
        int PositionLive { get;  }
        ILapInfo PreviousLap { get;  }
        double Prevspeed { get;  }
        double PrevTrackPct { get;  }
        double PrevTrackPctUpdate { get;  }
        event PropertyChangedEventHandler PropertyChanged;
        int Sector { get;  }
        double SectorBegin { get;  }
        float Speed { get;  }
        int Speed_kph { get;  }
        double TrackPct { get;  }
        SurfaceTypes TrackSurface { get;  }
    }
}
