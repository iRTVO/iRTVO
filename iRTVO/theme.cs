/*
 * theme.cs
 * 
 * Theme class:
 * 
 * Loads and stores the theme settings from the settings.ini. 
 * Available image filenames and overlay types are defined here.
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
// additional
using Ini;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.IO;

namespace iRTVO
{
    class Theme
    {

        public struct ObjectProperties
        {
            public int top;
            public int left;
            
            public int width;
            public int height;

            // sidepanel & results only
            public int size;
            public int itemHeight;

            public LabelProperties Num;
            public LabelProperties Name;
            public LabelProperties Diff;
            public LabelProperties Info;
        }

        public struct LabelProperties
        {
            public string text;

            /* Position */
            public int top;
            public int left;
            public int width;
            public int height;

            /* Font */
            public System.Windows.Media.FontFamily font;
            public int fontSize;
            public System.Windows.Media.SolidColorBrush fontColor;
            public System.Windows.FontWeight FontBold;
            public System.Windows.FontStyle FontItalic;
            public System.Windows.HorizontalAlignment TextAlign;
        }

        public enum overlayTypes
        {
            main = 0,
            driver = 1,
            sessionstate = 2,
            replay = 3,
            results = 4,
            sidepanel = 5,
            flaggreen = 6,
            flagyellow = 7,
            flagwhite = 8,
            flagcheckered = 9,
            lightsoff = 10,
            lightsred = 11,
            lightsgreen = 12,
            ticker = 13,
            laptime = 14
        }

        public static string[] filenames = new string[15] {
            "main.png",
            "driver.png",
            "sessionstate.png",
            "replay.png",
            "results.png",
            "sidepanel.png",
            "flag-green.png",
            "flag-yellow.png",
            "flag-white.png",
            "flag-checkered.png",
            "light-off.png",
            "light-red.png",
            "light-green.png",
            "ticker.png",
            "laptime.png"
        };

        public string name;
        public int width, height;
        public string path;
        private IniFile settings;

        public ObjectProperties driver;
        public ObjectProperties sidepanel;
        public ObjectProperties results;
        public ObjectProperties ticker;
        public LabelProperties resultsHeader;
        public LabelProperties resultsSubHeader;
        public LabelProperties sessionstateText;
        public LabelProperties laptimeText;

        public Dictionary<string, string> translation = new Dictionary<string, string>();

        public Dictionary<string, string> carClass = new Dictionary<string, string>();

        public Theme(string themeName)
        {
            path = "themes\\" + themeName;

            // if theme not found pick the first theme on theme dir
            if (!File.Exists(Directory.GetCurrentDirectory() + "\\" + path + "\\settings.ini"))
            {
                DirectoryInfo d = new DirectoryInfo(Directory.GetCurrentDirectory() + "\\themes\\");
                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    if (File.Exists(Directory.GetCurrentDirectory() + "\\themes\\" + di.Name + "\\settings.ini"))
                    {
                        themeName = di.Name;
                        break;
                    }
                }
            }

            settings = new IniFile(path + "\\settings.ini");

            name = themeName;
            width = Int32.Parse(getIniValue("General", "width"));
            height = Int32.Parse(getIniValue("General", "height"));

            sidepanel = loadProperties("Sidepanel");

            driver = loadProperties("Driver");

            results = loadProperties("Results");
            resultsHeader = loadLabelProperties("Results", "header");
            resultsSubHeader = loadLabelProperties("Results", "subheader");

            sessionstateText = loadLabelProperties("Sessionstate", "text");

            ticker = loadProperties("Ticker");

            laptimeText = loadLabelProperties("Laptime", "text");

            string[] translations = new string[13] {
                    "lap",
                    "laps",
                    "minutes",
                    "of",
                    "race",
                    "qualify",
                    "practice",
                    "out",
                    "remaining",
                    "gridding",
                    "pacelap",
                    "finallap",
                    "finishing"
            };

            foreach (string word in translations)
            {
                string translatedword = getIniValue("Translation", word);
                if(translatedword == "0") // default is the name of the property
                    translation.Add(word, word);
                else
                    translation.Add(word, translatedword);
            }

            // signs
            if (getIniValue("General", "switchsign") == "true")
            {
                translation.Add("ahead", "+");
                translation.Add("behind", "-");
            }
            else
            {
                translation.Add("ahead", "-");
                translation.Add("behind", "+");
            }

        }

        private ObjectProperties loadProperties(string prefix)
        {
            ObjectProperties o = new ObjectProperties();

            o.left = Int32.Parse(getIniValue(prefix, "left"));
            o.top = Int32.Parse(getIniValue(prefix, "top"));
            o.size = Int32.Parse(getIniValue(prefix, "number"));
            o.width = Int32.Parse(getIniValue(prefix, "width"));
            if(Int32.Parse(getIniValue(prefix, "itemheight")) > 0)
                o.height = o.size * Int32.Parse(getIniValue(prefix, "itemheight"));
            else
                o.height = Int32.Parse(getIniValue(prefix, "height"));
            o.itemHeight = Int32.Parse(getIniValue(prefix, "itemheight"));

            o.Num = loadLabelProperties(prefix, "num");
            o.Name = loadLabelProperties(prefix, "name");
            o.Diff = loadLabelProperties(prefix, "diff");
            o.Info = loadLabelProperties(prefix, "info");

            return o;
        }

        private LabelProperties loadLabelProperties(string prefix, string suffix)
        {
            LabelProperties lp = new LabelProperties();

            lp.text = getIniValue(prefix + "-" + suffix, "text");
            if (lp.text == "0")
                lp.text = "";

            lp.fontSize = Int32.Parse(getIniValue(prefix + "-" + suffix, "fontsize"));
            if (lp.fontSize == 0)
                lp.fontSize = 12;

            lp.left = Int32.Parse(getIniValue(prefix + "-" + suffix, "left"));
            lp.top = Int32.Parse(getIniValue(prefix + "-" + suffix, "top"));
            lp.width = Int32.Parse(getIniValue(prefix + "-" + suffix, "width"));
            lp.height = (int)((double)Int32.Parse(getIniValue(prefix + "-" + suffix, "fontsize")) * 1.5);

            if (File.Exists(@Directory.GetCurrentDirectory() + "\\" + path + "\\" + getIniValue(prefix + "-" + suffix, "font")))
            {
                lp.font = new System.Windows.Media.FontFamily(new Uri(Directory.GetCurrentDirectory() + "\\" + path + "\\" + getIniValue(prefix + "-" + suffix, "font")), getIniValue(prefix + "-" + suffix, "font"));
            }
            else if (getIniValue(prefix + "-" + suffix, "font") == "0")
                lp.font = new System.Windows.Media.FontFamily("Arial");
            else
                lp.font = new System.Windows.Media.FontFamily(getIniValue(prefix + "-" + suffix, "font"));
            
            if(getIniValue(prefix + "-" + suffix, "fontcolor") == "0")
                lp.fontColor = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            else
                lp.fontColor = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString(getIniValue(prefix + "-" + suffix, "fontcolor"));

            if (getIniValue(prefix + "-" + suffix, "fontbold") == "true")
                lp.FontBold = System.Windows.FontWeights.Bold;
            else
                lp.FontBold = System.Windows.FontWeights.Normal;

            if (getIniValue(prefix + "-" + suffix, "fontitalic") == "true")
                lp.FontItalic = System.Windows.FontStyles.Italic;
            else
                lp.FontItalic = System.Windows.FontStyles.Normal;

            switch (getIniValue(prefix + "-" + suffix, "align"))
            {
                case "center":
                    lp.TextAlign = System.Windows.HorizontalAlignment.Center;
                    break;
                case "right":
                    lp.TextAlign = System.Windows.HorizontalAlignment.Right;
                    break;
                default:
                    lp.TextAlign = System.Windows.HorizontalAlignment.Left;
                    break;
            }

            return lp;
        }

        public string getIniValue(string section, string key)
        {
            string retVal = settings.IniReadValue(section, key);

            if (retVal.Length == 0)
                return "0";
            else
                return retVal;
        }

        // *-name *-info
        public string[] getFormats(SharedData.DriverInfo driver)
        {
            TimeSpan laptime = DateTime.Now - driver.lastNewLap;

            string[] output = new string[14] {
                driver.name,
                driver.shortname,
                driver.initials,
                driver.license,
                driver.club,
                driver.car,
                getCarClass(driver.car), //driver.carclass.ToString(),
                (driver.numberPlate).ToString(),
                iRTVO.Overlay.floatTime2String(driver.fastestlap, true, false),
                iRTVO.Overlay.floatTime2String(driver.previouslap, true, false),
                "", // handled later // 10
                driver.completedlaps.ToString(),
                "", // fastlap speed
                "", // prev lap speed
            };

            if (laptime.TotalMinutes > 60)
                output[10] = "-.--";
            else if ((DateTime.Now - driver.lastNewLap).TotalSeconds > 5)
                output[10] = iRTVO.Overlay.floatTime2String((float)(DateTime.Now - driver.lastNewLap).TotalSeconds, true, false);
            else
                output[10] = iRTVO.Overlay.floatTime2String(driver.previouslap, true, false);

            double speedMultiplier = 3.6;

            if (Properties.Settings.Default.speedUnit == 1)
                speedMultiplier = 1609.344;

            if (driver.fastestlap > 0)
                output[12] = ((3600 * SharedData.track.length / (speedMultiplier * driver.fastestlap))).ToString("0.00");
            else
                output[12] = "-";
            if (driver.previouslap > 0)
                output[13] = ((3600 * SharedData.track.length / (speedMultiplier * driver.previouslap))).ToString("0.00");
            else
                output[13] = "-";

            return output;
        }

        public string[] getFormats(SharedData.SessionInfo session)
        {
            string[] output = new string[4] {
                session.laps.ToString(),
                session.lapsRemaining.ToString(),
                iRTVO.Overlay.floatTime2String(session.time, false, true),
                iRTVO.Overlay.floatTime2String(session.timeRemaining, false, true)
            };

            return output;
        }

        // *-num
        public string[] getFormats(SharedData.DriverInfo driver, int pos)
        {
            string[] output = new string[3] {
                (pos + 1).ToString(),
                (driver.numberPlate).ToString(),
                ""
            };

            if(pos == 10)
                output[2] = "11th"; 
            else if (pos == 11)
                output[2] = "12th";
            else if (((pos + 1) % 10) == 1)
                output[2] = (pos + 1).ToString() + "st";
            else if (((pos + 1) % 10) == 2)
                output[2] = (pos + 1).ToString() + "nd";
            else if (((pos + 1) % 10) == 3)
                output[2] = (pos + 1).ToString() + "rd";
            else
                output[2] = (pos + 1).ToString() + "th";

            return output;
        }

        /* unused
        public string[] getFormats(SharedData.LapInfo lapinfo)
        {
            string[] output = new string[2] {
                lapinfo.diff.ToString(),
                lapinfo.fastLap.ToString()
            };

            return output;
        }
        */

        private string getCarClass(string car)
        {
            if (car != null)
            {
                try
                {
                    return carClass[car];
                }
                catch
                {
                    string name = getIniValue("Multiclass", car);
                    if (name != "0")
                    {
                        carClass.Add(car, name);
                        return name;
                    }
                    else
                    {
                        carClass.Add(car, car);
                        return car;
                    }
                }
            }
            else
                return "";
        }
    }
}
