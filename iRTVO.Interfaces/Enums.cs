using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Interfaces
{
    public enum SessionTypes
    {
        invalid,
        practice,
        qualify,
        warmup,
        race
    }

    public enum SessionFlags
    {
        // global flags
        checkered,
        white,
        green,
        yellow,
        red,
        blue,
        debris,
        crossed,
        yellowWaving,
        oneLapToGreen,
        greenHeld,
        tenToGo,
        fiveToGo,
        randomWaving,
        caution,
        cautionWaving,

        // drivers black flags
        black,
        disqualify,
        servicible, // car is allowed service (not a flag)
        furled,
        repair,

        // start lights
        startHidden,
        startReady,
        startSet,
        startGo,

        // invalid
        invalid
    };

    public enum SessionStates
    {
        invalid,
        gridding,
        warmup,
        pacing,
        racing,
        checkered,
        cooldown
    }

    [Flags]
    public enum SessionStartLights : int
    {
        off = 1,    // hidden
        ready = 2,  // off
        set = 4,    // red
        go = 8     // green
    }

    public enum SessionEventTypes
    {
        bookmark,
        offtrack,
        fastlap,
        pit,
        flag,
        state,
        startlights
    }

    public enum SurfaceTypes
    {
        NotInWorld = -1,
        OffTrack,
        InPitStall,
        AproachingPits,
        OnTrack
    };

    public enum TriggerTypes
    {
        flagGreen,
        flagYellow,
        flagWhite,
        flagCheckered,
        lightsOff,
        lightsReady,
        lightsSet,
        lightsGo,
        replay,
        live,
        radioOn,
        radioOff,
        fastestlap,
        init
    }

    public enum DataOrders
    {
        position,
        liveposition,
        fastestlap,
        previouslap,
        classposition,
        classlaptime,
        points,
        trackposition
    }

    public enum DataSets
    {
        standing,
        followed,
        sessionstate,
        points,
        radio
    }

    public enum BookmarkTypes
    {
        Start = 0,
        Play,
        Stop
    }


}
