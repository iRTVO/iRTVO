using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
namespace iRTVO.Interfaces
{
    public interface ICameraGroup
    {
        int Id { get;  }
        string Name { get;  }
    }

    public interface ICameraInfo
    {
        int CurrentGroup { get;  }
        ICameraGroup FindId(int id);
        IList<ICameraGroup> Groups { get;  }
        event PropertyChangedEventHandler PropertyChanged;
        DateTime Updated { get;  }
        int WantedGroup { get;  }
    }
}
