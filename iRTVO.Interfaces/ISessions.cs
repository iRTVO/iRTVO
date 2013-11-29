using System;
using System.Collections.Generic;
using System.ComponentModel;
namespace iRTVO.Interfaces
{
    public interface ISessions
    {
        ISessionInfo CurrentSession { get;  }
        ISessionInfo findSessionType(SessionTypes type);
        bool Hosted { get;  }
        event PropertyChangedEventHandler PropertyChanged;
        int SessionId { get;  }
        IList<ISessionInfo> SessionList { get;  }        
        int SubSessionId { get;  }
    }
}
