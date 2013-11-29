using System;
using System.ComponentModel;
namespace iRTVO.Interfaces
{
    public interface ISessionEvent
    {
        string Description { get;  }
        IDriverInfo Driver { get;  }
        SessionEventTypes EventType { get;  }
        int Lap { get;  }
        event PropertyChangedEventHandler PropertyChanged;
        long ReplayPos { get;  }
        int Rewind { get;  }
        string Session { get;  }
        DateTime Timestamp { get;  }
    }
}
