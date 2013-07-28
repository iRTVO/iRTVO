using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptLibrary;

public interface IScript
{
    String init();
    Dictionary<String, String> getDriverInfo(iRTVO.DriverInfo driver);
}

namespace iRTVO
{
    class Scripting
    {
        Dictionary<String, IScript> scripts = new Dictionary<String, IScript>();

        public void loadScript(String filename)
        {
            IScript sc = CSScript.Evaluator.LoadFile<IScript>(filename);
            String scname = sc.init();
            scripts.Add(scname, sc);
        }
    }
}
