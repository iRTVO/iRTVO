using System;
namespace iRTVO.Interfaces
{
    public interface IDriverInfo
    {
        int CarClass { get;  }
        string CarClassName { get;  }
        int CarClassOrder { get;  }
        int CarId { get;  }
        int CarIdx { get;  }
        string Club { get;  }
        string[] ExternalData { get; }
        string Initials { get;  }
        int iRating { get;  }
        string Name { get;  }
        string NumberPlate { get;  }
        int NumberPlateInt { get; }
        int NumberPlatePadded { get; }
        string Shortname { get;  }
        string SR { get;  }
        int UserId { get;  }
        int TeamId { get; }         // KJ: additional Info provided by iRacing
        string TeamName { get; }    // KJ: additional Info provided by iRacing
        string CarName { get; }     // KJ: additional Info provided by iRacing
    }
}
