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

        public enum dataset
        {
            standing, 
            followed,
            sessionstate
        }

        public enum dataorder
        {
            position,
            laptime,
            classposition,
            classlaptime
        }

        public struct ObjectProperties
        {
            public int top;
            public int left;
            
            public int width;
            public int height;

            public dataset dataset;
            public dataorder dataorder;

            public LabelProperties[] labels;

            public int zIndex;
            public string name;

            // sidepanel & results only
            public int itemCount;
            public int itemHeight;
        }

        public struct LabelProperties
        {
            public string text;

            /* Position */
            public int top;
            public int left;

            /* Size */
            public int width;
            public int height;

            /* Font */
            public System.Windows.Media.FontFamily font;
            public int fontSize;
            public System.Windows.Media.SolidColorBrush fontColor;
            public System.Windows.FontWeight fontBold;
            public System.Windows.FontStyle fontItalic;
            public System.Windows.HorizontalAlignment textAlign;
        }

        public string name;
        public int width, height;
        public string path;
        private IniFile settings;

        public ObjectProperties[] objects;

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

            string tmp = getIniValue("General", "overlays");
            string[] overlays = tmp.Split(',');
            objects = new ObjectProperties[overlays.Length];

            for(int i = 0; i < overlays.Length; i++) {
                objects[i].name = getIniValue(overlays[i], "name");
                objects[i].dataorder = (dataorder)Enum.Parse(typeof(dataorder), getIniValue(overlays[i], "sort"));
                objects[i].width = Int32.Parse(getIniValue(overlays[i], "width"));
                objects[i].height = Int32.Parse(getIniValue(overlays[i], "height"));
                objects[i].left = Int32.Parse(getIniValue(overlays[i], "left"));
                objects[i].top = Int32.Parse(getIniValue(overlays[i], "top"));
                objects[i].zIndex = Int32.Parse(getIniValue(overlays[i], "zIndex"));
                objects[i].dataset = (dataset)Enum.Parse(typeof(dataset), getIniValue(overlays[i], "dataset"));

                // labels
                tmp = getIniValue(overlays[i], "labels");
                string[] labels = tmp.Split(',');
                objects[i].labels = new LabelProperties[labels.Length];
                for (int j = 0; j < labels.Length; j++) 
                    objects[i].labels[j] = loadLabelProperties(overlays[i], labels[j]);
                
            }

            // TODO load images, load buttons

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
        /*
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
        */
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
                lp.fontBold = System.Windows.FontWeights.Bold;
            else
                lp.fontBold = System.Windows.FontWeights.Normal;

            if (getIniValue(prefix + "-" + suffix, "fontitalic") == "true")
                lp.fontItalic = System.Windows.FontStyles.Italic;
            else
                lp.fontItalic = System.Windows.FontStyles.Normal;

            switch (getIniValue(prefix + "-" + suffix, "align"))
            {
                case "center":
                    lp.textAlign = System.Windows.HorizontalAlignment.Center;
                    break;
                case "right":
                    lp.textAlign = System.Windows.HorizontalAlignment.Right;
                    break;
                default:
                    lp.textAlign = System.Windows.HorizontalAlignment.Left;
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
        public string[] getFormats(SharedData.DriverInfo driver, int pos)
        {
            TimeSpan laptime = DateTime.Now - driver.lastNewLap;

            string[] output = new string[18] {
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
                "", // fastlap speed mph
                "", // prev lap speed mph
                "", // fastlap speed kph
                "", // prev lap speed kph
                pos.ToString(),
                "" // ordinal,
            };

            if (laptime.TotalMinutes > 60)
                output[10] = "-.--";
            else if ((DateTime.Now - driver.lastNewLap).TotalSeconds > 5)
                output[10] = iRTVO.Overlay.floatTime2String((float)(DateTime.Now - driver.lastNewLap).TotalSeconds, true, false);
            else
                output[10] = iRTVO.Overlay.floatTime2String(driver.previouslap, true, false);

            if (driver.fastestlap > 0)
            {
                output[12] = ((3600 * SharedData.track.length / (1609.344 * driver.fastestlap))).ToString("0.00");
                output[14] = ((3600 * SharedData.track.length / (3.6 * driver.fastestlap))).ToString("0.00");
            }
            else
            {
                output[12] = "-";
                output[14] = "-";
            }

            if (driver.previouslap > 0)
            {
                output[13] = ((3600 * SharedData.track.length / (1609.344 * driver.previouslap))).ToString("0.00");
                output[15] = ((3600 * SharedData.track.length / (1609.344 * driver.previouslap))).ToString("0.00");
            }
            else
            {
                output[13] = "-";
                output[15] = "-";
            }

            if(pos <= 0) // invalid input
                output[17] = "";
            else if (pos == 11)
                output[17] = "11th";
            else if (pos == 12)
                output[17] = "12th";
            else if ((pos % 10) == 1)
                output[17] = pos.ToString() + "st";
            else if ((pos % 10) == 2)
                output[17] = pos.ToString() + "nd";
            else if ((pos % 10) == 3)
                output[17] = pos.ToString() + "rd";
            else
                output[17] = pos.ToString() + "th";

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

        public string formatText(string text, int driver, int session)
        {
            int position = 0;

            Dictionary<string, int> formatMap = new Dictionary<string, int>()
            {
                {"name", 0},
                {"shortname", 1},
                {"initials", 2},
                {"license", 3},
                {"club", 4},
                {"car", 5},
                {"class", 6},
                {"carnum", 7},
                {"fastlap", 8},
                {"prevlap", 9},
                {"curlap", 10},
                {"lapnum", 11},
                {"speedfast_mph", 12},
                {"speedprev_mph", 13},
                {"speedfast_kph", 14},
                {"speedprev_kph", 15},
                {"position", 16},
                {"position_ord", 17},
            };

            StringBuilder t = new StringBuilder(text);

            foreach (KeyValuePair<string, int> pair in formatMap)
            {
                t.Replace("{" + pair.Key + "}", "{" + pair.Value + "}");
            }

            int i = 0;
            foreach (SharedData.LapInfo lapinfo in SharedData.standing[session])
            {
                i++;
                if (lapinfo.id == driver)
                {
                    position = i;
                }
            }

            return String.Format(t.ToString(), getFormats(SharedData.drivers[driver], position));
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
