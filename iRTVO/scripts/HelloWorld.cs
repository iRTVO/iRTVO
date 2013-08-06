using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Script : iRTVO.IScript
{
    public iRTVO.IHost Parent { set; get; }

    public String init()
    {
        Console.WriteLine("helloworld.init()");
        if (Parent != null)
        {
            Parent.Who();
            List<iRTVO.Sessions.SessionInfo.StandingsItem> standings = Parent.getStandings();
            foreach (iRTVO.Sessions.SessionInfo.StandingsItem si in standings)
                Console.WriteLine(si.Driver.Name);
            Console.WriteLine(standings.Count + " drivers");
        }

        // returns script name and does other initialization
        return "helloworld";
    }
}
