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
        }

        public struct LabelProperties
        {
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

        public string name;
        public int width, height;
        public string path;
        private IniFile settings;

        public ObjectProperties driver;
        public ObjectProperties sidepanel;
        public ObjectProperties results;
        public LabelProperties resultsHeader;
        public LabelProperties resultsSubHeader;
        public LabelProperties sessionstateText;

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

        }

        private ObjectProperties loadProperties(string prefix)
        {
            ObjectProperties o = new ObjectProperties();

            o.left = Int32.Parse(getIniValue(prefix, "left"));
            o.top = Int32.Parse(getIniValue(prefix, "top"));
            o.size = Int32.Parse(getIniValue(prefix, "number"));
            o.width = Int32.Parse(getIniValue(prefix, "width"));
            o.height = o.size * Int32.Parse(getIniValue(prefix, "itemheight"));
            o.itemHeight = Int32.Parse(getIniValue(prefix, "itemheight"));

            o.Num = loadLabelProperties(prefix, "num");
            o.Name = loadLabelProperties(prefix, "name");
            o.Diff = loadLabelProperties(prefix, "diff");

            return o;
        }

        private LabelProperties loadLabelProperties(string prefix, string suffix)
        {
            LabelProperties lp = new LabelProperties();

            lp.left = Int32.Parse(getIniValue(prefix + "-" + suffix, "left"));
            lp.top = Int32.Parse(getIniValue(prefix + "-" + suffix, "top"));
            lp.width = Int32.Parse(getIniValue(prefix + "-" + suffix, "width"));
            lp.height = Int32.Parse(getIniValue(prefix + "-" + suffix, "fontsize")) * 3;
            lp.fontSize = Int32.Parse(getIniValue(prefix + "-" + suffix, "fontsize"));
            if (File.Exists(@Directory.GetCurrentDirectory() + "\\" + path + "\\" + getIniValue(prefix + "-" + suffix, "font")))
            {
                lp.font = new System.Windows.Media.FontFamily(new Uri(Directory.GetCurrentDirectory() + "\\" + path + "\\" + getIniValue(prefix + "-" + suffix, "font")), getIniValue(prefix + "-" + suffix, "font"));
            }
            else
                lp.font = new System.Windows.Media.FontFamily(getIniValue(prefix + "-" + suffix, "font"));
            lp.fontColor = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString(getIniValue(prefix + "-" + suffix, "fontcolor"));

            if (getIniValue(prefix + "-" + suffix, "fontbold") == "true")
                lp.FontBold = System.Windows.FontWeights.Bold;

            if (getIniValue(prefix + "-" + suffix, "fontitalic") == "true")
                lp.FontItalic = System.Windows.FontStyles.Italic;

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
    }
}
