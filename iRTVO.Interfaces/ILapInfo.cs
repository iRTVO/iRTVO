using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace iRTVO.Interfaces
{
    public interface ISector
    {
        double Begin { get;  }
        int Num { get;  }
        float Speed { get;  }
        float Time { get;  }
    }

    public interface ILapInfo
    {
        int ClassPosition { get;  }
        float Gap { get;  }
        string Gap_HR { get;  }
        int GapLaps { get;  }
        int LapNum { get;  }
        float LapTime { get;  }
        string LapTime_HR { get; }
        int Position { get;  }
        event PropertyChangedEventHandler PropertyChanged;
        int ReplayPos { get;  }
        IList<ISector> SectorTimes { get;  }
        double SessionTime { get;  }
    }
}
