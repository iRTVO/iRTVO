//css_include interface.cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Script
{
    public String init()
    {
        // returns script name and does other initialization
        return "helloworld";
    }

    Dictionary<String, String> getDriverInfo(DriverInfo driver)
    {
        Dictionary<String, String> retval = new Dictionary<String, String>();

        retval.Add("example1", "Hello world!");
        retval.Add("example2", driver.Name);

        return retval;
    }
}
