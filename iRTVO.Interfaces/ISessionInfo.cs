using System;
using System.Collections.Generic;
using System.ComponentModel;
namespace iRTVO.Interfaces
{
    public interface ISessionInfo
    {
        int CautionLaps { get;  }
        int Cautions { get;  }
        int CurrentReplayPosition { get;  }
        Single PreviousFastestLap { get; }
        float FastestLap { get;  }
        IDriverInfo FastestLapDriver { get;  }
        int FastestLapNum { get;  }
        IStandingsItem FindDriver(int caridx);
        IStandingsItem FindPosition(int pos, DataOrders order);
        IStandingsItem FindPosition(int pos, DataOrders order, string classname);
        int FinishLine { get;  }
        SessionFlags Flag { get;  }
        IStandingsItem FollowedDriver { get;  }
        int getClassCarCount(string className);
        IStandingsItem getClassLeader(string className);
        int getClassLivePosition(IDriverInfo driver);
        int getClassPosition(IDriverInfo driver);
        IStandingsItem getLeader();
        IStandingsItem getLiveLeader();
        int Id { get;  }
        int LapsComplete { get;  }
        int LapsRemaining { get;  }
        int LapsTotal { get;  }
        int LeadChanges { get;  }
        event PropertyChangedEventHandler PropertyChanged;
        double SessionLength { get;  }
        double SessionStartTime { get;  }
        IList<IStandingsItem> Standings { get;  }
        SessionStartLights StartLight { get;  }
        SessionStates State { get;  }
        double Time { get;  }
        double TimeRemaining { get;  }
        SessionTypes Type { get;  }
        // KJ: some number juggling
        Int32 GetFrameNumForTime(double searchtime);
        double GetTimeForFrameNum(Int32 searchframe);
    }
}
