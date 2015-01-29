using System;
namespace iRTVO.Interfaces
{
    public interface ITrackInfo
    {
        float AirPressure { get;  }
        float AirTemperature { get;  }
        float Altitude { get;  }
        string City { get;  }
        string Country { get;  }
        int Fog { get;  }
        int Humidity { get;  }
        int Id { get;  }
        float Length { get;  }
        string Name { get;  }
        string BaseName { get; }     // KJ: additional Info provided by iRacing
        string ShortName { get; }    // KJ: additional Info provided by iRacing
        string Config { get; }       // KJ: additional Info provided by iRacing
        string Sky { get;  }
        float TrackTemperature { get;  }
        int Turns { get;  }
        float WindDirection { get;  }
        float WindSpeed { get;  }
    }
}
