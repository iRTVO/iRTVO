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
using System.Windows.Media;

namespace iRTVO
{
    class Theme
    {

        public enum dataset
        {
            standing, 
            followed,
            sessionstate,
            points
        }

        public enum ThemeTypes
        {
            Overlay,
            Image,
            Ticker,
            Video
        }

        public enum ButtonActions
        {
            show,
            hide,
            toggle
        }

        public enum flags
        {
            none = 0,
            green,
            yellow,
            white,
            checkered
        }

        public enum lights 
        {
            none = 0,
            off,
            red,
            green
        }

        public enum direction
        {
            down,
            up,
            left,
            right
        }

        public enum sessionType
        {
            none = 0,
            practice,
            qualify,
            race
        }

        public struct ObjectProperties
        {
            public int top;
            public int left;
            
            public int width;
            public int height;

            public dataset dataset;
            public dataorder dataorder;
            public string carclass;

            public LabelProperties[] labels;

            public int zIndex;
            public string name;

            // sidepanel & results only
            public int itemCount;
            public int itemSize;
            public int page;
            public direction direction;
            public int offset;
            public int maxpages;
            public int skip;
            public int pagecount;
            public string leadervalue;

            public Boolean visible;
            public Boolean presistent;
        }

        public struct ImageProperties
        {
            public string filename;
            public int zIndex;
            public Boolean visible;
            public Boolean presistent;
            public string name;

            public Boolean dynamic;
            public string defaultFile;

            public int top;
            public int left;

            public int width;
            public int height;

        }

        public struct VideoProperties
        {
            public int top;
            public int left;

            public int width;
            public int height;

            public string filename;

            public int zIndex;
            public Boolean visible;

            public string name;

            public Boolean loop;
            public Boolean playing;
            public Boolean presistent;
        }

        public struct SoundProperties
        {
            public string name;
            public string filename;
            public Boolean loop;
            public Boolean playing;
        }

        public struct TickerProperties
        {
            public int top;
            public int left;
            
            public int width;
            public int height;

            public int speed;

            public dataset dataset;
            public dataorder dataorder;
            //public int externalDataorderCol;
            public string carclass;

            public LabelProperties[] labels;
            public LabelProperties header;
            public LabelProperties footer;
            public string leadervalue;

            public int zIndex;
            public string name;

            public Boolean fillVertical;

            public Boolean visible;
            public Boolean presistent;
        }

        public struct ButtonProperties
        {
            public string name;
            public string text;
            public string[][] actions;
            public int row;
            public int delay;
            public Boolean delayLoop;
            public Boolean active;
            public DateTime pressed;
        }

        public struct TriggerProperties
        {
            public string name;
            public string[][] actions;
        }

        public struct LabelProperties
        {
            public string text;

            // Position
            public int top;
            public int left;
            public int[] padding;

            // Size
            public int width;
            public int height;

            // Font
            public System.Windows.Media.FontFamily font;
            public int fontSize;
            
            public System.Windows.FontWeight fontBold;
            public System.Windows.FontStyle fontItalic;
            public System.Windows.HorizontalAlignment textAlign;

            // Colors
            public string backgroundImage;
            public string defaultBackgroundImage;
            public bool dynamic;
            public System.Windows.Media.SolidColorBrush backgroundColor;
            public System.Windows.Media.SolidColorBrush fontColor;

            // Misc.
            public Boolean uppercase;
            public int offset;
            public sessionType session;
            public int rounding;

        }

        public string name;
        public int width, height;
        public string path;

        public int[] pointschema;
        public int pointscol;
        public Single minscoringdistance;

        private IniFile settings;

        public ObjectProperties[] objects;
        public ImageProperties[] images;
        public TickerProperties[] tickers;
        public ButtonProperties[] buttons;
        public TriggerProperties[] triggers;
        public VideoProperties[] videos;
        public SoundProperties[] sounds;

        public Dictionary<string, string> translation = new Dictionary<string, string>();
        public Dictionary<int, string> carClass = new Dictionary<int, string>();
        public Dictionary<int, string> carName = new Dictionary<int, string>();

        public Theme(string themeName)
        {
            path = "themes\\" + themeName;

            // if theme not found pick the first theme on theme dir
            if (!File.Exists(Directory.GetCurrentDirectory() + "\\" + path + "\\settings.ini"))
            {
                System.Windows.MessageBox.Show("Could not find theme named '" + themeName + "' from '" + Directory.GetCurrentDirectory() + "\\" + path + "\\'");

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

            path = "themes\\" + themeName;

            settings = new IniFile(path + "\\settings.ini");

            name = themeName;
            width = Int32.Parse(getIniValue("General", "width"));
            height = Int32.Parse(getIniValue("General", "height"));

            // point schema
            pointscol = Int32.Parse(getIniValue("General", "pointscol"));
            minscoringdistance = Single.Parse(getIniValue("General", "minscoringdistance"))/100;
            if (minscoringdistance == 0.0f)
                minscoringdistance = 1.0f;
            string[] pointschemastr = getIniValue("General", "pointschema").Split(',');
            pointschema = new Int32[pointschemastr.Length];
            for (int i = 0; i < pointschemastr.Length; i++)
                pointschema[i] = Int32.Parse(pointschemastr[i]);

            // load objects
            string tmp = getIniValue("General", "overlays");
            string[] overlays;
            if (tmp != "0")
            {
                overlays = tmp.Split(',');
                objects = new ObjectProperties[overlays.Length];
            }
            else
            {
                objects = new ObjectProperties[0];
                overlays = new string[0];
            }

            for(int i = 0; i < overlays.Length; i++) {

                objects[i].name = overlays[i];
                objects[i].width = Int32.Parse(getIniValue("Overlay-" + overlays[i], "width"));
                objects[i].height = Int32.Parse(getIniValue("Overlay-" + overlays[i], "height"));
                objects[i].left = Int32.Parse(getIniValue("Overlay-" + overlays[i], "left"));
                objects[i].top = Int32.Parse(getIniValue("Overlay-" + overlays[i], "top"));
                objects[i].zIndex = Int32.Parse(getIniValue("Overlay-" + overlays[i], "zIndex"));
                objects[i].offset = Int32.Parse(getIniValue("Overlay-" + overlays[i], "offset"));
                objects[i].dataset = (dataset)Enum.Parse(typeof(dataset), getIniValue("Overlay-" + overlays[i], "dataset"));

                if (getIniValue("Overlay-" + overlays[i], "class") != "0")
                    objects[i].carclass = getIniValue("Overlay-" + overlays[i], "class");
                else
                    objects[i].carclass = null;

                if (getIniValue("Overlay-" + overlays[i], "fixed") == "true")
                    objects[i].presistent = true;
                else
                    objects[i].presistent = false;

                if (getIniValue("Overlay-" + overlays[i], "leader") != "0")
                    objects[i].leadervalue = getIniValue("Overlay-" + overlays[i], "leader");
                else
                    objects[i].leadervalue = null;

                int extraHeight = 0;

                // load labels
                tmp = getIniValue("Overlay-" + overlays[i], "labels");
                string[] labels = tmp.Split(',');
                objects[i].labels = new LabelProperties[labels.Length];
                for (int j = 0; j < labels.Length; j++)
                {
                    objects[i].labels[j] = loadLabelProperties("Overlay-" + overlays[i], labels[j]);
                    if (objects[i].labels[j].height > extraHeight)
                        extraHeight = objects[i].labels[j].height;
                }

                if (objects[i].dataset == dataset.standing || objects[i].dataset == dataset.points)
                {
                    objects[i].itemCount = Int32.Parse(getIniValue("Overlay-" + overlays[i], "number"));
                    objects[i].itemSize = Int32.Parse(getIniValue("Overlay-" + overlays[i], "itemHeight"));
                    objects[i].itemSize += Int32.Parse(getIniValue("Overlay-" + overlays[i], "itemsize"));
                    objects[i].height = (objects[i].itemCount * objects[i].itemSize) + extraHeight;
                    objects[i].page = -1;
                    objects[i].direction = (direction)Enum.Parse(typeof(direction), getIniValue("Overlay-" + overlays[i], "direction"));
                    objects[i].offset = Int32.Parse(getIniValue("Overlay-" + overlays[i], "offset"));
                    objects[i].maxpages = Int32.Parse(getIniValue("Overlay-" + overlays[i], "maxpages"));
                    objects[i].skip = Int32.Parse(getIniValue("Overlay-" + overlays[i], "skip"));
                }

                switch (getIniValue("Overlay-" + overlays[i], "dataorder"))
                {
                    case "fastestlap":
                        objects[i].dataorder = dataorder.fastestlap;
                        break;
                    case "previouslap":
                        objects[i].dataorder = dataorder.previouslap;
                        break;
                    case "class":
                        objects[i].dataorder = dataorder.previouslap;
                        break;
                    case "points":
                        objects[i].dataorder = dataorder.points;
                        break;
                    case "liveposition":
                        objects[i].dataorder = dataorder.liveposition;
                        break;
                    default:
                        objects[i].dataorder = dataorder.position;
                        break;
                }
                objects[i].visible = false;
            }

            // load images
            tmp = getIniValue("General", "images");
            string[] files;
            if (tmp != "0")
            {
                files = tmp.Split(',');
                images = new ImageProperties[files.Length];
            }
            else
            {
                images = new ImageProperties[0];
                files = new string[0];
            }
            
            for (int i = 0; i < files.Length; i++)
            {
                images[i].filename = getIniValue("Image-" + files[i], "filename");
                images[i].zIndex = Int32.Parse(getIniValue("Image-" + files[i], "zIndex"));
                images[i].visible = false;
                images[i].name = files[i];

                images[i].width = Int32.Parse(getIniValue("Image-" + files[i], "width"));
                images[i].height = Int32.Parse(getIniValue("Image-" + files[i], "height"));
                images[i].left = Int32.Parse(getIniValue("Image-" + files[i], "left"));
                images[i].top = Int32.Parse(getIniValue("Image-" + files[i], "top"));

                if (getIniValue("Image-" + files[i], "dynamic") == "true")
                {
                    images[i].dynamic = true;
                    images[i].defaultFile = getIniValue("Image-" + files[i], "default");
                }
                else
                    images[i].dynamic = false;

                if (getIniValue("Image-" + files[i], "fixed") == "true")
                    images[i].presistent = true;
                else
                    images[i].presistent = false;
            }

            // load videos
            tmp = getIniValue("General", "videos");
            if (tmp != "0")
            {
                files = tmp.Split(',');
                videos = new VideoProperties[files.Length];
            }
            else
            {
                videos = new VideoProperties[0];
                files = new string[0];
            }

            for (int i = 0; i < files.Length; i++)
            {
                videos[i].filename = getIniValue("Video-" + files[i], "filename");
                videos[i].zIndex = Int32.Parse(getIniValue("Video-" + files[i], "zIndex"));
                videos[i].width = Int32.Parse(getIniValue("Video-" + files[i], "width"));
                videos[i].height = Int32.Parse(getIniValue("Video-" + files[i], "height"));
                videos[i].left = Int32.Parse(getIniValue("Video-" + files[i], "left"));
                videos[i].top = Int32.Parse(getIniValue("Video-" + files[i], "top"));
                videos[i].visible = false;
                videos[i].playing = false;
                videos[i].name = files[i];

                if (getIniValue("Video-" + files[i], "fixed") == "true")
                    videos[i].presistent = true;
                else
                    videos[i].presistent = false;

                if (getIniValue("Video-" + files[i], "loop") == "true")
                    videos[i].loop = true;
                else
                    videos[i].loop = false;
            }


            // load sounds
            tmp = getIniValue("General", "sounds");

            if (tmp != "0")
            {
                files = tmp.Split(',');
                sounds = new SoundProperties[files.Length];
            }
            else
            {
                sounds = new SoundProperties[0];
                files = new string[0];
            }

            for (int i = 0; i < files.Length; i++)
            {
                sounds[i].filename = getIniValue("Sound-" + files[i], "filename");
                sounds[i].playing = false;
                sounds[i].name = files[i];

                if (getIniValue("Sound-" + files[i], "loop") == "true")
                    sounds[i].loop = true;
                else
                    sounds[i].loop = false;
            }

            // load tickers
            tmp = getIniValue("General", "tickers");
            string[] tickersnames;
            if (tmp != "0")
            {
                tickersnames = tmp.Split(',');
                tickers = new TickerProperties[tickersnames.Length];
            }
            else
            {
                tickers = new TickerProperties[0];
                tickersnames = new string[0];
            }

            for (int i = 0; i < tickersnames.Length; i++)
            {
                tickers[i].name = tickersnames[i];
                //tickers[i].dataorder = (dataorder)Enum.Parse(typeof(dataorder), getIniValue("Ticker-" + tickersnames[i], "sort"));
                tickers[i].width = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "width"));
                tickers[i].height = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "height"));
                tickers[i].left = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "left"));
                tickers[i].top = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "top"));
                tickers[i].zIndex = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "zIndex"));
                tickers[i].dataset = (dataset)Enum.Parse(typeof(dataset), getIniValue("Ticker-" + tickersnames[i], "dataset"));

                if (getIniValue("Ticker-" + tickersnames[i], "class") != "0")
                    tickers[i].carclass = getIniValue("Ticker-" + tickersnames[i], "class");
                else
                    tickers[i].carclass = null;

                switch (getIniValue("Ticker-" + tickersnames[i], "dataorder"))
                {
                    case "fastestlap":
                        tickers[i].dataorder = dataorder.fastestlap;
                        break;
                    case "previouslap":
                        tickers[i].dataorder = dataorder.previouslap;
                        break;
                    case "class":
                        tickers[i].dataorder = dataorder.classposition;
                        break;
                    case "classposition":
                        tickers[i].dataorder = dataorder.classposition;
                        break;
                    default:
                        tickers[i].dataorder = dataorder.position;
                        break;
                }

                if (getIniValue("Ticker-" + tickersnames[i], "speed") != "0")
                    tickers[i].speed = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "speed"));
                else
                    tickers[i].speed = 120;

                if (getIniValue("Ticker-" + tickersnames[i], "header") != "0")
                    tickers[i].header = loadLabelProperties("Ticker-" + tickersnames[i], getIniValue("Ticker-" + tickersnames[i], "header"));

                if (getIniValue("Ticker-" + tickersnames[i], "footer") != "0")
                    tickers[i].footer = loadLabelProperties("Ticker-" + tickersnames[i], getIniValue("Ticker-" + tickersnames[i], "footer"));

                if (getIniValue("Ticker-" + tickersnames[i], "fillvertical") == "true")
                    tickers[i].fillVertical = true;
                else
                    tickers[i].fillVertical = false;

                if (getIniValue("Ticker-" + tickersnames[i], "fixed") == "true")
                    tickers[i].presistent = true;
                else
                    tickers[i].presistent = false;

                if (getIniValue("Ticker-" + overlays[i], "leader") != "0")
                    tickers[i].leadervalue = getIniValue("Ticker-" + overlays[i], "leader");
                else
                    tickers[i].leadervalue = null;

                // load labels
                tmp = getIniValue("Ticker-" + tickersnames[i], "labels");
                string[] labels = tmp.Split(',');
                tickers[i].labels = new LabelProperties[labels.Length];
                for (int j = 0; j < labels.Length; j++)
                    tickers[i].labels[j] = loadLabelProperties("Ticker-" + tickersnames[i], labels[j]);

                tickers[i].visible = false;
            }


            // load buttons
            tmp = getIniValue("General", "buttons");
            string[] btns = tmp.Split(',');
            buttons = new ButtonProperties[btns.Length];
            for (int i = 0; i < btns.Length; i++)
            {
                foreach (ThemeTypes type in Enum.GetValues(typeof(ThemeTypes)))
                {
                    buttons[i].name = btns[i];
                    buttons[i].text = getIniValue("Button-" + btns[i], "text");
                    buttons[i].row = Int32.Parse(getIniValue("Button-" + btns[i], "row"));
                    buttons[i].delay = Int32.Parse(getIniValue("Button-" + btns[i], "delay"));
                    buttons[i].active = false;
                    buttons[i].pressed = DateTime.Now;
                    buttons[i].actions = new string[Enum.GetValues(typeof(ButtonActions)).Length][];

                    if (getIniValue("Button-" + btns[i], "loop") == "true")
                        buttons[i].delayLoop = true;
                    else
                        buttons[i].delayLoop = false;

                    foreach (ButtonActions action in Enum.GetValues(typeof(ButtonActions)))
                    {
                        tmp = getIniValue("Button-" + btns[i], action.ToString());
                        if (tmp != "0")
                        {
                            string[] objs = tmp.Split(',');

                            buttons[i].actions[(int)action] = new string[objs.Length];
                            for (int j = 0; j < objs.Length; j++)
                            {
                                buttons[i].actions[(int)action][j] = objs[j];
                            }
                        }
                        else
                        {
                            buttons[i].actions[(int)action] = null;
                        }
                    }
                }
            }

            // load triggers
            //tmp = getIniValue("General", "triggers");
            //string[] trgrs = tmp.Split(',');
            triggers = new TriggerProperties[Enum.GetValues(typeof(TriggerTypes)).Length];
            //for (int i = 0; i < trgrs.Length; i++)
            int trigidx = 0;
            foreach (TriggerTypes trigger in Enum.GetValues(typeof(TriggerTypes)))
            {

                foreach (ThemeTypes type in Enum.GetValues(typeof(ThemeTypes)))
                {
                    triggers[trigidx].name = trigger.ToString();
                    triggers[trigidx].actions = new string[Enum.GetValues(typeof(ButtonActions)).Length][];

                    foreach (ButtonActions action in Enum.GetValues(typeof(ButtonActions)))
                    {
                        tmp = getIniValue("Trigger-" + trigger.ToString(), action.ToString());
                        if (tmp != "0")
                        {
                            string[] objs = tmp.Split(',');

                            triggers[trigidx].actions[(int)action] = new string[objs.Length];
                            for (int j = 0; j < objs.Length; j++)
                            {
                                triggers[trigidx].actions[(int)action][j] = objs[j];
                            }
                        }
                        else
                        {
                            triggers[trigidx].actions[(int)action] = null;
                        }
                    }
                }
                trigidx++;
            }

            SharedData.refreshButtons = true;

            string[] translations = new string[20] { // default translations
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
                    "finishing",
                    "leader",
                    "invalid",
                    "replay",
                    "Clear",
                    "Partly Cloudy",
                    "Mostly Cloudy",
                    "Overcast",
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
            lp.height = Int32.Parse(getIniValue(prefix + "-" + suffix, "height"));
            if(lp.height == 0)
                lp.height = (int)((double)Int32.Parse(getIniValue(prefix + "-" + suffix, "fontsize")) * 1.5);

            lp.padding = new int[4] {0, 0, 0, 0};
            lp.padding[0] = Int32.Parse(getIniValue(prefix + "-" + suffix, "padding-left"));
            lp.padding[1] = Int32.Parse(getIniValue(prefix + "-" + suffix, "padding-top"));
            lp.padding[2] = Int32.Parse(getIniValue(prefix + "-" + suffix, "padding-right"));
            lp.padding[3] = Int32.Parse(getIniValue(prefix + "-" + suffix, "padding-bottom"));

            if (getIniValue(prefix + "-" + suffix, "font") == "0")
                lp.font = new System.Windows.Media.FontFamily("Arial");
            else
                lp.font = new System.Windows.Media.FontFamily(new Uri(Directory.GetCurrentDirectory() + "\\" + path + "\\"), "./#" + getIniValue(prefix + "-" + suffix, "font") + ", " + getIniValue(prefix + "-" + suffix, "font") + ", Arial");

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

            if (getIniValue(prefix + "-" + suffix, "uppercase") == "true")
                lp.uppercase = true;
            else
                lp.uppercase = false;

            lp.offset = Int32.Parse(getIniValue(prefix + "-" + suffix, "offset"));
            lp.session = (sessionType)Enum.Parse(typeof(sessionType), getIniValue(prefix + "-" + suffix, "session"));

            if (getIniValue(prefix + "-" + suffix, "background") == "0")
                lp.backgroundImage = null;
            else
                lp.backgroundImage = getIniValue(prefix + "-" + suffix, "background");

            if (getIniValue(prefix + "-" + suffix, "bgcolor") == "0") {
                Color bgcolor = Color.FromArgb(0, 0, 0, 0);
                lp.backgroundColor = new System.Windows.Media.SolidColorBrush(bgcolor);
            }
            else
                lp.backgroundColor = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString(getIniValue(prefix + "-" + suffix, "bgcolor"));

            if (getIniValue(prefix + "-" + suffix, "defaultbackground") == "0")
                lp.defaultBackgroundImage = null;
            else
                lp.defaultBackgroundImage = getIniValue(prefix + "-" + suffix, "defaultbackground");

            if (getIniValue(prefix + "-" + suffix, "dynamic") == "true")
                lp.dynamic = true;
            else
                lp.dynamic = false;

            lp.rounding = Int32.Parse(getIniValue(prefix + "-" + suffix, "rounding"));
            if (lp.rounding <= 0)
                lp.rounding = 0;
            else if (lp.rounding >= 3)
                lp.rounding = 3;

            return lp;
        }

        public string getIniValue(string section, string key)
        {
            string retVal = settings.IniReadValue(section, key);

            if (retVal.Length == 0)
                return "0";
            else
                return retVal.Trim();
        }

        // *-name *-info
        public string[] getFollowedFormats(Sessions.SessionInfo.StandingsItem standing, Sessions.SessionInfo session, Int32 rounding)
        {
            Double laptime = SharedData.currentSessionTime - standing.Begin;

            string[] output = new string[67] {
                standing.Driver.Name,
                standing.Driver.Shortname,
                standing.Driver.Initials,
                standing.Driver.SR,
                standing.Driver.Club,
                getCar(standing.Driver.CarId),
                getCarClass(standing.Driver.CarId), //driver.carclass.ToString(),
                (standing.Driver.NumberPlate).ToString(),
                iRTVO.Overlay.floatTime2String(standing.FastestLap, rounding, false),
                iRTVO.Overlay.floatTime2String(standing.PreviousLap.LapTime, rounding, false),
                "", // currentlap (live) // 10
                standing.CurrentLap.LapNum.ToString(),
                "", // fastlap speed mph
                "", // prev lap speed mph
                "", // fastlap speed kph
                "", // prev lap speed kph
                standing.Position.ToString(),
                ordinate(standing.Position), // ordinal
                "",
                standing.LapsLed.ToString(),
                standing.Driver.UserId.ToString(), //20
                "",
                "",
                "",
                "",
                (standing.Speed * 3.6).ToString("0"),
                (standing.Speed * 2.237).ToString("0"),
                "",
                "",
                "",
                standing.PitStops.ToString(), //30
                iRTVO.Overlay.floatTime2String(standing.PitStopTime, rounding, false),
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "", // 40
                "",
                "",
                "",
                "",
                "",
                standing.ClassLapsLed.ToString(),
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                standing.PositionLive.ToString(),
                ordinate(standing.PositionLive),
                "",
                "",
                standing.Driver.iRating.ToString(),
            };



            if (standing.FastestLap < 5)
                output[8] = translation["invalid"];

            if (standing.PreviousLap.LapTime < 5)
                output[9] = translation["invalid"];

            if (laptime/60 > 60)
                output[10] = translation["invalid"];
            else if (((SharedData.currentSessionTime - standing.Begin) < 5))
            {
                if (standing.PreviousLap.LapTime < 5)
                    output[10] = translation["invalid"];
                else
                    output[10] = iRTVO.Overlay.floatTime2String(standing.PreviousLap.LapTime, rounding, false);
            }
            //else if (standing.OnTrack == false)
            else if (standing.TrackSurface == Sessions.SessionInfo.StandingsItem.SurfaceType.NotInWorld)
            {
                output[10] = iRTVO.Overlay.floatTime2String(standing.FastestLap, rounding, false);
            }
            else {
                output[10] = iRTVO.Overlay.floatTime2String((float)(SharedData.currentSessionTime - standing.Begin), rounding, false);
            }

            if (standing.FastestLap > 0)
            {
                output[12] = ((3600 * SharedData.Track.length / (1609.344 * standing.FastestLap))).ToString("0.00");
                output[14] = ((3600 * SharedData.Track.length / (3.6 * standing.FastestLap))).ToString("0.00");
            }
            else
            {
                output[12] = "-";
                output[14] = "-";
            }

            if (standing.PreviousLap.LapTime > 0)
            {
                output[13] = ((3600 * SharedData.Track.length / (1609.344 * standing.PreviousLap.LapTime))).ToString("0.00");
                output[15] = ((3600 * SharedData.Track.length / (1609.344 * standing.PreviousLap.LapTime))).ToString("0.00");
            }
            else
            {
                output[13] = "-";
                output[15] = "-";
            }

            /*if ((DateTime.Now - standing.OffTrackSince).TotalMilliseconds > 1000 && standing.OnTrack == false && SharedData.allowRetire)*/
            if (standing.TrackSurface == Sessions.SessionInfo.StandingsItem.SurfaceType.NotInWorld && 
                SharedData.allowRetire && 
                (DateTime.Now - standing.OffTrackSince).TotalSeconds > 1)
            {
                output[18] = translation["out"];
            }
            else
            {
                if (standing.Position == 1)
                {
                    if (session.Type == Sessions.SessionInfo.sessionType.race)
                        output[18] = "";//iRTVO.Overlay.floatTime2String((float)standing.CurrentLap.SessionTime, rounding, true); //translation["leader"];
                    else
                        output[18] = iRTVO.Overlay.floatTime2String(standing.FastestLap, rounding, false);
                }
                else if (standing.PreviousLap.GapLaps > 0 && session.Type == Sessions.SessionInfo.sessionType.race)
                {
                    /*
                    if (session.State == Sessions.SessionInfo.sessionState.cooldown ||
                        (session.State == Sessions.SessionInfo.sessionState.checkered && standing.CurrentTrackPct > session.LapsComplete))
                    {
                        output[18] = translation["behind"] + standing.FindLap(session.LapsComplete).GapLaps + " ";
                        if (standing.FindLap(session.LapsComplete).GapLaps > 1)
                            output[18] += translation["laps"];
                        else
                            output[18] += translation["lap"];
                    }
                    else
                    {
                    */
                        output[18] = translation["behind"] + standing.PreviousLap.GapLaps + " ";
                        if (standing.PreviousLap.GapLaps > 1)
                            output[18] += translation["laps"];
                        else
                            output[18] += translation["lap"];
                    //}
                }
                else/* if (SharedData.standing[SharedData.overlaySession].Length > 0 && SharedData.standing[SharedData.overlaySession][0].fastLap > 0)*/
                {
                    if (session.Type == Sessions.SessionInfo.sessionType.race)
                    {
                        /*
                        if (session.State == Sessions.SessionInfo.sessionState.cooldown ||
                        (session.State == Sessions.SessionInfo.sessionState.checkered && standing.CurrentTrackPct > session.LapsComplete))
                        {
                            output[18] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.FindLap(session.LapsComplete).Gap), rounding, false);
                        }
                        else
                        {
                            output[18] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.PreviousLap.Gap), rounding, false);
                        }
                         * */
                        output[18] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.PreviousLap.Gap), rounding, false);
                    }
                    else if (standing.FastestLap <= 1)
                        output[18] = translation["invalid"];
                    else
                        output[18] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.FastestLap - session.FastestLap), rounding, false);    
                }
            }

            // interval
            if (standing.Position > 1) // not leader
            {
                Sessions.SessionInfo.StandingsItem infront = new Sessions.SessionInfo.StandingsItem();
                infront = session.FindPosition(standing.Position - 1, dataorder.position);

                if (session.Type == Sessions.SessionInfo.sessionType.race)
                {
                    if ((infront.CurrentTrackPct - standing.CurrentTrackPct) > 1)
                    {
                        output[21] = translation["behind"] + standing.PreviousLap.GapLaps + " ";
                        if (standing.PreviousLap.GapLaps > 1)
                            output[21] += translation["laps"];
                        else
                            output[21] += translation["lap"];
                    }
                    else
                    {
                        output[21] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.PreviousLap.Gap - infront.PreviousLap.Gap), rounding, false);
                    }
                }
                else // qualify and practice
                {
                    if (standing.FastestLap <= 1)
                    {
                        output[21] = translation["invalid"];
                    }
                    else 
                    {
                        output[21] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.FastestLap - infront.FastestLap), rounding, false);
                    }
                }

                output[22] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.PreviousLap.Gap - infront.PreviousLap.Gap), rounding, false);
            }
            else // leader
            {

                if (session.Type == Sessions.SessionInfo.sessionType.race)
                {
                    output[21] = ""; //iRTVO.Overlay.floatTime2String((float)standing.CurrentLap.SessionTime, rounding, true); //translation["leader"];
                    output[22] = output[21];
                }
                else // qualify and practice
                {
                    output[21] = iRTVO.Overlay.floatTime2String(standing.FastestLap, rounding, false);
                    output[22] = output[21];
                }
            }

            if (session.Type == Sessions.SessionInfo.sessionType.race)
            {
                output[23] = translation["behind"] + standing.GapLive_HR(rounding);
                output[24] = translation["behind"] + standing.IntervalLive_HR(rounding);
            }
            else {
                output[23] = output[18];
                output[24] = output[22];
            }

            // sectors
            if (SharedData.SelectedSectors.Count > 0)
            {
                for(int i = 0; i <= 2; i++) {
                    if (standing.Sector <= 0) // first sector, show previous lap times
                    {
                        if (i < standing.PreviousLap.SectorTimes.Count) 
                        {
                            LapInfo.Sector sector = standing.PreviousLap.SectorTimes.Find(s => s.Num.Equals(i));
                            if(sector != null)
                            {

                                output[27 + i] = iRTVO.Overlay.floatTime2String(sector.Time, rounding, false);
                                output[32 + i] = (sector.Speed * 3.6).ToString("0.0");
                                output[35 + i] = (sector.Speed * 2.237).ToString("0.0");
                            }
                            else
                            {
                                output[27 + i] = "";
                                output[32 + i] = "";
                                output[35 + i] = "";
                            }
                        }
                        else
                        {
                            output[27 + i] = "";
                            output[32 + i] = "";
                            output[35 + i] = "";
                        }
                    }
                    else
                    {
                        if (i < standing.CurrentLap.SectorTimes.Count)
                        {
                            LapInfo.Sector sector = standing.CurrentLap.SectorTimes.Find(s => s.Num.Equals(i));
                            if(sector != null) 
                            {
                                output[27 + i] = iRTVO.Overlay.floatTime2String(sector.Time, rounding, false);
                                output[32 + i] = (sector.Speed * 3.6).ToString("0.0");
                                output[35 + i] = (sector.Speed * 2.237).ToString("0.0");
                            }
                            else
                            {
                                output[27 + i] = "";
                                output[32 + i] = "";
                                output[35 + i] = "";
                            }
                        }
                        else
                        {
                            output[27 + i] = "";
                            output[32 + i] = "";
                            output[35 + i] = "";
                        }
                    }
                }
            }

            // position gain
            Sessions.SessionInfo qualifySession = SharedData.Sessions.findSessionType(Sessions.SessionInfo.sessionType.qualify);
            if (qualifySession.Type != Sessions.SessionInfo.sessionType.invalid)
            {
                int qualifyPos = qualifySession.FindDriver(standing.Driver.CarIdx).Position;
                if((qualifyPos - standing.Position) > 0)
                    output[38] = "+" + (qualifyPos - standing.Position).ToString();
                else
                    output[38] = (qualifyPos - standing.Position).ToString();

                output[47] = qualifyPos.ToString();
                output[50] = ordinate(qualifyPos);

                Int32 classqpos = qualifySession.getClassPosition(standing.Driver);
                output[48] = classqpos.ToString();
                output[49] = ordinate(classqpos);
            }

            // highest/lowest position
            output[51] = standing.HighestPosition.ToString();
            output[52] = ordinate(standing.HighestPosition);
            output[53] = standing.LowestPosition.ToString();
            output[54] = ordinate(standing.LowestPosition);

            output[55] = standing.HighestClassPosition.ToString();
            output[56] = ordinate(standing.HighestClassPosition);
            output[57] = standing.LowestClassPosition.ToString();
            output[58] = ordinate(standing.LowestClassPosition);

            /*
            // pititemr
            if ((DateTime.Now - standing.PitStopBegin).TotalSeconds > 1)
                output[31] = iRTVO.Overlay.floatTime2String(standing.PitStopTime, rounding, false);
            else
            {
                output[31] = iRTVO.Overlay.floatTime2String((float)(DateTime.Now - standing.PitStopBegin).TotalSeconds, rounding, false);
            }
            */

            // multiclass
            /*
                {"classposition", 39},
                {"classposition_ord", 40},
                {"classpositiongain", 41},
                {"classgap", 42},
                {"classlivegap", 43},
                {"classinterval", 44},
                {"classliveinterval", 45},
             */
            int classpos = session.getClassPosition(standing.Driver);
            Sessions.SessionInfo.StandingsItem classLeader = session.getClassLeader(standing.Driver.CarClassName);
            output[39] = classpos.ToString();
            output[40] = ordinate(classpos);
            output[43] = standing.ClassGapLive_HR;
            output[45] = standing.ClassIntervalLive_HR;

            if (qualifySession.Type != Sessions.SessionInfo.sessionType.invalid)
            {
                IEnumerable<Sessions.SessionInfo.StandingsItem> query = qualifySession.Standings.Where(s => s.Driver.CarClassName == standing.Driver.CarClassName).OrderBy(s => s.Position);
                Int32 position = 1;
                Int32 qualifyPos = 0;
                foreach (Sessions.SessionInfo.StandingsItem si in query)
                {
                    if (si.Driver.CarIdx == standing.Driver.CarIdx)
                    {
                        qualifyPos = position;
                        break;
                    }
                    else
                        position++;
                }

                if ((qualifyPos - classpos) > 0)
                    output[41] = "+" + (qualifyPos - classpos).ToString();
                else
                    output[41] = (qualifyPos - classpos).ToString();
            }

            if (standing.TrackSurface == Sessions.SessionInfo.StandingsItem.SurfaceType.NotInWorld &&
                SharedData.allowRetire &&
                (DateTime.Now - standing.OffTrackSince).TotalSeconds > 1)
            {
                output[42] = translation["out"];
            }
            else
            {
                float gap = classLeader.PreviousLap.Gap - standing.PreviousLap.Gap;
                int gaplaps = classLeader.PreviousLap.LapNum - standing.PreviousLap.LapNum;
                if (classpos == 1)
                {
                    if (session.Type == Sessions.SessionInfo.sessionType.race)
                        output[42] = ""; //iRTVO.Overlay.floatTime2String((float)standing.CurrentLap.SessionTime, rounding, true); //translation["leader"];
                    else
                        output[42] = iRTVO.Overlay.floatTime2String(standing.FastestLap, rounding, false);
                }
                else if (gaplaps > 0 && session.Type == Sessions.SessionInfo.sessionType.race)
                {
                    output[42] = translation["behind"] + gaplaps + " ";
                    if (gaplaps > 1)
                        output[42] += translation["laps"];
                    else
                        output[42] += translation["lap"];
                    //}
                }
                else/* if (SharedData.standing[SharedData.overlaySession].Length > 0 && SharedData.standing[SharedData.overlaySession][0].fastLap > 0)*/
                {
                    if (session.Type == Sessions.SessionInfo.sessionType.race)
                    {
                        if (session.State == Sessions.SessionInfo.sessionState.cooldown ||
                        (session.State == Sessions.SessionInfo.sessionState.checkered && standing.CurrentTrackPct > session.LapsComplete))
                        {
                            output[42] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.FindLap(session.LapsComplete).Gap), rounding, false);
                        }
                        else
                        {
                            output[42] = translation["behind"] + iRTVO.Overlay.floatTime2String(gap, rounding, false);
                        }
                    }
                    else if (standing.FastestLap <= 1)
                        output[42] = translation["invalid"];
                    else
                        output[42] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.FastestLap - classLeader.FastestLap), rounding, false);

                }
            }

            // interval
            if (standing.PreviousLap.Position > 1)
            {
                Sessions.SessionInfo.StandingsItem infront = new Sessions.SessionInfo.StandingsItem();
                infront = session.FindPosition(standing.Position - 1, dataorder.position, standing.Driver.CarClassName);

                if (session.Type == Sessions.SessionInfo.sessionType.race)
                {
                    if ((infront.CurrentTrackPct - standing.CurrentTrackPct) > 1)
                    {
                        output[44] = translation["behind"] + standing.PreviousLap.GapLaps + " ";
                        if (standing.PreviousLap.GapLaps > 1)
                            output[44] += translation["laps"];
                        else
                            output[44] += translation["lap"];
                    }
                    else
                    {
                        output[44] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.PreviousLap.Gap - infront.PreviousLap.Gap), rounding, false);
                    }
                }
                else
                {
                    if (standing.FastestLap <= 1)
                    {
                        output[44] = translation["invalid"];
                    }
                    else
                    {
                        output[44] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.FastestLap - infront.FastestLap), rounding, false);
                    }
                }

                output[44] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.PreviousLap.Gap - infront.PreviousLap.Gap), rounding, false);
            }
            else
            {

                if (session.Type == Sessions.SessionInfo.sessionType.race)
                {
                    output[44] = ""; // iRTVO.Overlay.floatTime2String((float)standing.CurrentLap.SessionTime, rounding, true); //translation["leader"];
                }
                else
                {
                    output[44] = iRTVO.Overlay.floatTime2String(standing.FastestLap, rounding, false);
                }
            }

            if (session.Type == Sessions.SessionInfo.sessionType.race)
            {
                output[23] = translation["behind"] + standing.GapLive_HR(rounding);
                output[24] = translation["behind"] + standing.IntervalLive_HR(rounding);
            }
            else
            {
                output[23] = output[18];
                output[24] = output[22];
            }

            // points
            if (SharedData.externalCurrentPoints.ContainsKey(standing.Driver.UserId))
            {
                output[59] = SharedData.externalCurrentPoints[standing.Driver.UserId].ToString();
                int pos = 0;
                foreach (KeyValuePair<int, int> item in SharedData.externalCurrentPoints.OrderByDescending(key => key.Value))
                {
                    pos++;
                    if (item.Key == standing.Driver.UserId)
                    {
                        output[60] = pos.ToString();
                        output[61] = ordinate(pos);
                        break;
                    }
                }
            }
            else
            {
                output[59] = "0";
                output[60] = SharedData.externalCurrentPoints.Count().ToString();
                output[61] = ordinate(SharedData.externalCurrentPoints.Count());
            }

            int classlivepos = session.getClassLivePosition(standing.Driver);
            output[64] = classlivepos.ToString();
            output[65] = ordinate(classlivepos);

            string[] extrenal;
            if (SharedData.externalData.ContainsKey(standing.Driver.UserId))
            {
                extrenal = SharedData.externalData[standing.Driver.UserId];
            }
            else
                extrenal = new string[0];


            

            string[] merged = new string[output.Length + extrenal.Length];
            Array.Copy(output, 0, merged, 0, output.Length);
            Array.Copy(extrenal, 0, merged, output.Length, extrenal.Length);

            return merged;
        }

        public string formatFollowedText(LabelProperties label, Sessions.SessionInfo.StandingsItem standing, Sessions.SessionInfo session)
        {
            string output = "";

            Dictionary<string, int> formatMap = new Dictionary<string, int>()
            {
                {"fullname", 0},
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
                {"gap", 18},
                {"lapsled", 19},
                {"driverid", 20},
                {"interval", 21},
                {"interval_time", 22},
                {"livegap", 23},
                {"liveinterval", 24},
                {"livespeed_kph", 25},
                {"livespeed_mph", 26},
                {"sector1", 27},
                {"sector2", 28},
                {"sector3", 29},
                {"pitstops", 30},
                {"pitstoptime", 31},
                {"sector1_speed_kph", 32},
                {"sector2_speed_kph", 33},
                {"sector3_speed_kph", 34},
                {"sector1_speed_mph", 35},
                {"sector2_speed_mph", 36},
                {"sector3_speed_mph", 37},
                {"positiongain", 38},
                {"classposition", 39},
                {"classposition_ord", 40},
                {"classpositiongain", 41},
                {"classgap", 42},
                {"classlivegap", 43},
                {"classinterval", 44},
                {"classliveinterval", 45},
                {"classlapsled", 46},
                {"startposition", 47},
                {"classstartposition", 48},
                {"classstartposition_ord", 49},
                {"startposition_ord", 50},
                {"highestposition", 51},
                {"highestposition_ord", 52},
                {"lowestposition", 53},
                {"lowestposition_ord", 54},
                {"classhighestposition", 55},
                {"classhighestposition_ord", 56},
                {"classlowestposition", 57},
                {"classlowestposition_ord", 58},
                {"points", 59},
                {"points_pos", 60},
                {"points_pos_ord", 61},
                {"liveposition", 62},
                {"liveposition_ord", 63},
                {"classliveposition", 64},
                {"classliveposition_ord", 65},
                {"irating", 66}
            };

            StringBuilder t = new StringBuilder(label.text);

            foreach (KeyValuePair<string, int> pair in formatMap)
            {
                t.Replace("{" + pair.Key + "}", "{" + pair.Value + "}");
            }

            if (SharedData.externalData.ContainsKey(standing.Driver.UserId))
            {
                for (int i = 0; i < SharedData.externalData[standing.Driver.UserId].Length; i++)
                {
                    t.Replace("{external:" + i + "}", "{" + (formatMap.Keys.Count + i) + "}");
                }
            }

            // remove leftovers
            string format = t.ToString();
            int start, end;
            do
            {
                start = format.IndexOf("{external:", 0);
                if (start >= 0)
                {
                    end = format.IndexOf('}', start) + 1;
                    format = format.Remove(start, end - start);
                }
            } while (start >= 0);


            if (standing.Driver.CarIdx < 0)
            {
                output = String.Format(format, getFollowedFormats(standing, session, label.rounding));
            }
            else if (SharedData.themeDriverCache[standing.Driver.CarIdx][label.rounding] == null)
            {
                string[] cache = getFollowedFormats(standing, session, label.rounding);
                SharedData.themeDriverCache[standing.Driver.CarIdx][label.rounding] = cache;
                try
                {
                    output = String.Format(format, cache);
                }
                catch (FormatException)
                {
                    output = "[invalid]";
                }
                SharedData.cacheMiss++;

            }
            else
            {
                try
                {
                    output = String.Format(format, SharedData.themeDriverCache[standing.Driver.CarIdx][label.rounding]);
                }
                catch (FormatException)
                {
                    output = "[invalid]";
                }

                SharedData.cacheHit++;
            }

            output = output.Replace("\\n", System.Environment.NewLine);

            if (label.uppercase)
                return output.ToUpper();
            else
                return output;
        }


        public string[] getSessionstateFormats(Sessions.SessionInfo session, Int32 rounding)
        {
            string[] output = new string[33] {
                session.LapsTotal.ToString(),
                session.LapsRemaining.ToString(),
                iRTVO.Overlay.floatTime2String((float)session.SessionLength, rounding, true),
                iRTVO.Overlay.floatTime2String((float)session.TimeRemaining, rounding, true),
                "",
                iRTVO.Overlay.floatTime2String((float)session.Time, rounding, true),
                "",
                SharedData.Track.name,
                round(SharedData.Track.length * 0.6214 / 1000, rounding),
                round(SharedData.Track.length / 1000, rounding),
                session.Cautions.ToString(),
                session.CautionLaps.ToString(),
                session.LeadChanges.ToString(),
                "",
                (session.LapsComplete + 1).ToString(),
                SharedData.Track.turns.ToString(),
                SharedData.Track.city,
                SharedData.Track.country,
                Math.Round(SharedData.Track.altitude).ToString(),
                translation[SharedData.Track.sky],
                Math.Round(SharedData.Track.tracktemp, rounding).ToString(),
                Math.Round(SharedData.Track.airtemp, rounding).ToString(),
                SharedData.Track.humidity.ToString(),
                SharedData.Track.fog.ToString(),
                Math.Round(SharedData.Track.airpressure, rounding).ToString(),
                Math.Round(SharedData.Track.windspeed, rounding).ToString(),
                Math.Round(360 * SharedData.Track.winddirection / (2*Math.PI), rounding).ToString(),
                Math.Round(SharedData.Track.altitude * 3.281, rounding).ToString(),
                Math.Round(SharedData.Track.tracktemp * 9/5 + 32, rounding).ToString(),
                Math.Round(SharedData.Track.airtemp * 9/5 + 32, rounding).ToString(),
                Math.Round(SharedData.Track.airpressure * 1.333224, rounding).ToString(),
                Math.Round(SharedData.Track.windspeed * 1.943844, rounding).ToString(),
                Math.Round(SharedData.Track.windspeed * 3.6, rounding).ToString(),
            };

            if (session.SessionLength == float.MaxValue)
                output[2] = "-.--";

            if(session.TimeRemaining > 600000)
                output[3] = "-.--";

            if ((session.LapsComplete) < 0)
                output[4] = "0";
            else
                output[4] = session.LapsComplete.ToString();

            // lap counter
            if (session.LapsTotal == Int32.MaxValue || session.LapsTotal < 1)
            {
                if (session.State == Sessions.SessionInfo.sessionState.checkered) // session ending
                    output[6] = translation["finishing"];
                else // normal
                    output[6] = iRTVO.Overlay.floatTime2String((float)SharedData.Sessions.CurrentSession.TimeRemaining, rounding, true);
            }
            else if (session.State == Sessions.SessionInfo.sessionState.gridding)
            {
                output[6] = translation["gridding"];
            }
            else if (session.State == Sessions.SessionInfo.sessionState.pacing)
            {
                output[6] = translation["pacelap"];
            }
            else
            {
                int currentlap = session.LapsComplete;

                if (session.LapsRemaining < 1 && session.LapsComplete > 0)
                {
                    output[6] = translation["finishing"];
                }
                else if (session.LapsRemaining == 1)
                {
                    output[6] = translation["finallap"];
                }
                else if (session.LapsRemaining <= SharedData.settings.LapCountdownFrom)
                { // x laps remaining
                    output[6] = String.Format("{0} {1} {2}",
                        session.LapsRemaining,
                        translation["laps"],
                        translation["remaining"]
                    );
                }
                else // normal behavior
                {
                    if (currentlap < 0)
                        currentlap = 0;

                    output[6] = String.Format("{0} {1} {2} {3}",
                        translation["lap"],
                        (currentlap + 1),
                        translation["of"],
                        session.LapsTotal
                    );

                }
            }

            switch (session.Type)
            {
                case Sessions.SessionInfo.sessionType.race:
                    output[13] = translation["race"];
                    break;
                case Sessions.SessionInfo.sessionType.qualify:
                    output[13] = translation["qualify"];
                    break;
                case Sessions.SessionInfo.sessionType.practice:
                    output[13] = translation["practice"];
                    break;
                default:
                    output[13] = "";
                    break;
            }

            return output;
        }

        public string formatSessionstateText(LabelProperties label, int session)
        {
            string[] cache;
            Dictionary<string, int> formatMap = new Dictionary<string, int>()
            {
                {"lapstotal", 0},
                {"lapsremaining", 1},
                {"timetotal", 2},
                {"timeremaining", 3},
                {"lapscompleted", 4},
                {"timepassed", 5},
                {"lapcounter", 6},
                {"trackname", 7},
                {"tracklen_mi", 8},
                {"tracklen_km", 9},
                {"cautions", 10},
                {"cautionlaps", 11},
                {"leadchanges", 12},
                {"sessiontype", 13},
                {"currentlap", 14},
                {"turns", 15},
                {"city", 16},
                {"country", 17},
                {"altitude_m", 18},
                {"sky", 19},
                {"tracktemp_c", 20},
                {"airtemp_c", 21},
                {"humidity", 22},
                {"fog", 23},
                {"airpressure_hg", 24},
                {"windspeed_ms", 25},
                {"winddir_deg", 26},
                {"altitude_ft", 27},
                {"tracktemp_f", 28},
                {"airtemp_f", 29},
                {"airpressure_hpa", 30},
                {"windspeed_kt", 31},
                {"windspeed_kph", 32},
            };

            StringBuilder t = new StringBuilder(label.text);

            foreach (KeyValuePair<string, int> pair in formatMap)
            {
                t.Replace("{" + pair.Key + "}", "{" + pair.Value + "}");
            }

            if (SharedData.themeSessionStateCache.Length != formatMap.Count)
            {
                cache = getSessionstateFormats(SharedData.Sessions.SessionList[session], label.rounding);
                SharedData.themeSessionStateCache = cache;
                SharedData.cacheMiss++;
            }
            else
            {
                cache = SharedData.themeSessionStateCache;
                SharedData.cacheHit++;
            }

            if (label.uppercase)
                return String.Format(t.ToString(), cache).ToUpper().Replace("\\n", System.Environment.NewLine);
            else
                return String.Format(t.ToString(), cache).Replace("\\n", System.Environment.NewLine);

        }

        public string getCarClass(int car)
        {
            try
            {
                return carClass[car];
            }
            catch
            {
                string filename = Directory.GetCurrentDirectory() + "\\themes\\" + this.name + "\\cars.ini";
                if (!File.Exists(filename))
                    filename = Directory.GetCurrentDirectory() + "\\cars.ini";

                if (File.Exists(filename))
                {
                    IniFile carNames;
                    carNames = new IniFile(filename);

                    // update class order
                    string[] order = carNames.IniReadValue("Multiclass", "order").Split(',');
                    SharedData.ClassOrder.Clear();

                    for (Int32 i = 0; i < order.Length; i++)
                        SharedData.ClassOrder.Add(order[i], i);

                    string name = carNames.IniReadValue("Multiclass", car.ToString());

                    if (name.Length > 0)
                    {
                        carClass.Add(car, name);
                        return name;
                    }
                    else
                    {
                        carClass.Add(car, car.ToString());
                        return car.ToString();
                    }
                }
                else
                {
                    carClass.Add(car, car.ToString());
                    return car.ToString();
                }
            }
        }

        public string getCar(int car)
        {
            try
            {
                return carName[car];
            }
            catch
            {
                string filename = Directory.GetCurrentDirectory() + "\\themes\\" + this.name + "\\cars.ini";
                if (!File.Exists(filename))
                    filename = Directory.GetCurrentDirectory() + "\\cars.ini";

                if (File.Exists(filename))
                {
                    IniFile carNames;

                    carNames = new IniFile(filename);
                    string name = carNames.IniReadValue("Cars", car.ToString());

                    if (name.Length > 0)
                    {
                        carName.Add(car, name);
                        return name;
                    }
                    else
                    {
                        carName.Add(car, car.ToString());
                        return car.ToString();
                    }
                }
                else
                {
                    carName.Add(car, car.ToString());
                    return car.ToString();
                }
            }
        }

        public void readExternalData()
        {
            SharedData.externalData.Clear();
            SharedData.externalPoints.Clear();
            SharedData.externalCurrentPoints.Clear();

            string filename = Directory.GetCurrentDirectory() + "\\themes\\" + this.name + "\\data.csv";
            if (File.Exists(filename))
            {
                string[] lines = System.IO.File.ReadAllLines(filename);

                foreach (string line in lines)
                {
                    string[] split = line.Split(';');
                    int custId = Int32.Parse(split[0]);
                    string[] data = new string[split.Length-1];

                    if (custId > 0)
                    {
                        Array.Copy(split, 1, data, 0, data.Length);
                        SharedData.externalData.Add(custId, data);

                        if (pointscol > 0)
                        {
                            int number;
                            bool result = Int32.TryParse(data[pointscol], out number);
                            if (result && data[pointscol].Length > 0)
                                SharedData.externalPoints.Add(custId, number);
                            // don't add drivers who don't have points set
                            //else
                            //    SharedData.externalPoints.Add(custId, 0);
                        }
                    }
                }
            }
        }

        public static LabelProperties setLabelPosition(ObjectProperties obj, LabelProperties lp, int i)
        {
            switch (obj.direction)
            {
                case direction.down:
                    lp.top += i * obj.itemSize;
                    break;
                case direction.left:
                    lp.left -= i * obj.itemSize;
                    break;
                case direction.right:
                    lp.left += i * obj.itemSize;
                    break;
                case direction.up:
                    lp.top -= i * obj.itemSize;
                    break;
            }

            return lp;
        }

        private string ordinate(int num)
        {
            if (num <= 0) // invalid input
                return "-";
            else if (num == 11)
                return "11th";
            else if (num == 12)
                return "12th";
            else if (num == 13)
                return "13th";
            else if ((num % 10) == 1)
                return num.ToString() + "st";
            else if ((num % 10) == 2)
                return num.ToString() + "nd";
            else if ((num % 10) == 3)
                return num.ToString() + "rd";
            else
                return num.ToString() + "th";
        }

        public static string round(Double x, Int32 decimals)
        {
            return x.ToString("F"+ decimals.ToString());
        }
    }
}
