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
            classlaptime,
            external
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
            toggle,
            replay
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
            public int externalDataorderCol;

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

            public flags flag;
            public lights light;
            public Boolean replay;

            public Boolean dynamic;
            public string defaultFile;

            public int top;
            public int left;

            public int width;
            public int height;

        }

        public struct VideoProperties
        {
            public string filename;
            public int zIndex;
            public Boolean visible;
            public string name;
            public flags flag;
            public lights light;
            public Boolean replay;
            public Boolean playing;
            public Boolean presistent;
        }

        public struct TickerProperties
        {
            public int top;
            public int left;
            
            public int width;
            public int height;

            public dataset dataset;
            public dataorder dataorder;
            public int externalDataorderCol;

            public LabelProperties[] labels;

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
        }

        public struct LabelProperties
        {
            public string text;

            // Position
            public int top;
            public int left;

            // Size
            public int width;
            public int height;

            // Font
            public System.Windows.Media.FontFamily font;
            public int fontSize;
            public System.Windows.Media.SolidColorBrush fontColor;
            public System.Windows.FontWeight fontBold;
            public System.Windows.FontStyle fontItalic;
            public System.Windows.HorizontalAlignment textAlign;

            public Boolean uppercase;
            public int offset;
            public sessionType session;

        }

        public string name;
        public int width, height;
        public string path;
        private IniFile settings;

        public ObjectProperties[] objects;
        public ImageProperties[] images;
        public TickerProperties[] tickers;
        public ButtonProperties[] buttons;
        public VideoProperties[] videos;

        public Dictionary<string, string> translation = new Dictionary<string, string>();
        public Dictionary<string, string> carClass = new Dictionary<string, string>();
        public Dictionary<string, string> carName = new Dictionary<string, string>();

        private Boolean MsgBoxShown = false;

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

            // load objects
            string tmp = getIniValue("General", "overlays");
            string[] overlays = tmp.Split(',');
            objects = new ObjectProperties[overlays.Length];

            for(int i = 0; i < overlays.Length; i++) {

                objects[i].name = overlays[i];
                objects[i].dataorder = (dataorder)Enum.Parse(typeof(dataorder), getIniValue("Overlay-" + overlays[i], "sort"));
                objects[i].width = Int32.Parse(getIniValue("Overlay-" + overlays[i], "width"));
                objects[i].height = Int32.Parse(getIniValue("Overlay-" + overlays[i], "height"));
                objects[i].left = Int32.Parse(getIniValue("Overlay-" + overlays[i], "left"));
                objects[i].top = Int32.Parse(getIniValue("Overlay-" + overlays[i], "top"));
                objects[i].zIndex = Int32.Parse(getIniValue("Overlay-" + overlays[i], "zIndex"));
                objects[i].dataset = (dataset)Enum.Parse(typeof(dataset), getIniValue("Overlay-" + overlays[i], "dataset"));

                if (getIniValue("Overlay-" + overlays[i], "fixed") == "true")
                    objects[i].presistent = true;
                else
                    objects[i].presistent = false;

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

                if (objects[i].dataset == dataset.standing)
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

                objects[i].visible = false;
            }

            // load images
            tmp = getIniValue("General", "images");
            string[] files = tmp.Split(',');
            images = new  ImageProperties[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                images[i].filename = getIniValue("Image-" + files[i], "filename");
                images[i].zIndex = Int32.Parse(getIniValue("Image-" + files[i], "zIndex"));
                images[i].visible = false;
                images[i].name = files[i];
                images[i].flag = (flags)Enum.Parse(typeof(flags), getIniValue("Image-" + files[i], "flag"));
                images[i].light = (lights)Enum.Parse(typeof(lights), getIniValue("Image-" + files[i], "light"));

                images[i].width = Int32.Parse(getIniValue("Image-" + files[i], "width"));
                images[i].height = Int32.Parse(getIniValue("Image-" + files[i], "height"));
                images[i].left = Int32.Parse(getIniValue("Image-" + files[i], "left"));
                images[i].top = Int32.Parse(getIniValue("Image-" + files[i], "top"));

                if (getIniValue("Image-" + files[i], "replay") == "true")
                    images[i].replay = true;
                else
                    images[i].replay = false;

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
            files = tmp.Split(',');
            videos = new VideoProperties[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                videos[i].filename = getIniValue("Video-" + files[i], "filename");
                videos[i].zIndex = Int32.Parse(getIniValue("Video-" + files[i], "zIndex"));
                videos[i].visible = false;
                videos[i].playing = false;
                videos[i].name = files[i];
                videos[i].flag = (flags)Enum.Parse(typeof(flags), getIniValue("Video-" + files[i], "flag"));
                videos[i].light = (lights)Enum.Parse(typeof(lights), getIniValue("Video-" + files[i], "light"));

                if (getIniValue("Video-" + files[i], "replay") == "true")
                    videos[i].replay = true;
                else
                    videos[i].replay = false;

                if (getIniValue("Video-" + files[i], "fixed") == "true")
                    videos[i].presistent = true;
                else
                    videos[i].presistent = false;
            }

            // load tickers
            tmp = getIniValue("General", "tickers");
            string[] tickersnames = tmp.Split(',');
            tickers = new TickerProperties[tickersnames.Length];
            for (int i = 0; i < tickersnames.Length; i++)
            {
                tickers[i].name = tickersnames[i];
                tickers[i].dataorder = (dataorder)Enum.Parse(typeof(dataorder), getIniValue("Ticker-" + tickersnames[i], "sort"));
                tickers[i].width = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "width"));
                tickers[i].height = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "height"));
                tickers[i].left = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "left"));
                tickers[i].top = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "top"));
                tickers[i].zIndex = Int32.Parse(getIniValue("Ticker-" + tickersnames[i], "zIndex"));
                tickers[i].dataset = (dataset)Enum.Parse(typeof(dataset), getIniValue("Ticker-" + tickersnames[i], "dataset"));

                if (getIniValue("Ticker-" + tickersnames[i], "fillvertical") == "true")
                    tickers[i].fillVertical = true;
                else
                    tickers[i].fillVertical = false;

                if (getIniValue("Ticker-" + tickersnames[i], "fixed") == "true")
                    tickers[i].presistent = true;
                else
                    tickers[i].presistent = false;

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
                    buttons[i].actions = new string[Enum.GetValues(typeof(ButtonActions)).Length][];

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

            SharedData.refreshButtons = true;

            string[] translations = new string[15] { // default translations
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
                    "invalid"
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

            if (getIniValue(prefix + "-" + suffix, "uppercase") == "true")
                lp.uppercase = true;
            else
                lp.uppercase = false;

            lp.offset = Int32.Parse(getIniValue(prefix + "-" + suffix, "offset"));
            lp.session = (sessionType)Enum.Parse(typeof(sessionType), getIniValue(prefix + "-" + suffix, "session"));

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
        public string[] getFollewedFormats(StandingsItem standing, SessionInfo session)
        {
            TimeSpan laptime = DateTime.Now - standing.CurrentLap.Begin;
            //SharedData.LapInfo leader = new SharedData.LapInfo();

            /*
            if(SharedData.standing[SharedData.overlaySession].Length > 0)
                leader = SharedData.standing[SharedData.overlaySession][0];
            
            if (driver.GetType() != typeof(SharedData.DriverInfo))
                driver = new SharedData.DriverInfo();
            
            if (lapinfo.GetType() != typeof(SharedData.LapInfo))
                lapinfo = new SharedData.LapInfo();
            */
            

            string[] output = new string[30] {
                standing.Driver.Name,
                standing.Driver.Shortname,
                standing.Driver.Initials,
                standing.Driver.License,
                standing.Driver.Club,
                getCar(standing.Driver.Car),
                getCarClass(standing.Driver.Car), //driver.carclass.ToString(),
                (standing.Driver.NumberPlate).ToString(),
                iRTVO.Overlay.floatTime2String(standing.FastestLap, true, false),
                iRTVO.Overlay.floatTime2String(standing.PreviousLap.LapTime, true, false),
                "", // currentlap (live) // 10
                standing.CurrentLap.LapNum.ToString(),
                "", // fastlap speed mph
                "", // prev lap speed mph
                "", // fastlap speed kph
                "", // prev lap speed kph
                standing.Position.ToString(),
                "", // ordinal
                "",
                standing.LapsLed.ToString(),
                standing.Driver.UserId.ToString(),
                "",
                "",
                "",
                "",
                (standing.Speed * 3.6).ToString("0"),
                (standing.Speed * 2.237).ToString("0"),
                "",
                "",
                "",
            };

            if (standing.FastestLap < 5)
                output[8] = translation["invalid"];

            if (standing.PreviousLap.LapTime < 5)
                output[9] = translation["invalid"];

            if (laptime.TotalMinutes > 60)
                output[10] = translation["invalid"];
            else if (((DateTime.Now - standing.CurrentLap.Begin).TotalSeconds < 5))
            {
                if (standing.PreviousLap.LapTime < 5)
                    output[10] = translation["invalid"];
                else
                    output[10] = iRTVO.Overlay.floatTime2String(standing.PreviousLap.LapTime, true, false);
            }
            //else if (standing.OnTrack == false)
            else if (standing.TrackSurface == SurfaceType.NotInWorld)
            {
                output[10] = iRTVO.Overlay.floatTime2String(standing.FastestLap, true, false);
            }
            else {
                output[10] = iRTVO.Overlay.floatTime2String((float)(DateTime.Now - standing.CurrentLap.Begin).TotalSeconds, true, false);
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

            if (standing.Position <= 0) // invalid input
                output[17] = "-";
            else if (standing.Position == 11)
                output[17] = "11th";
            else if (standing.Position == 12)
                output[17] = "12th";
            else if (standing.Position == 13)
                output[17] = "13th";
            else if ((standing.Position % 10) == 1)
                output[17] = standing.Position.ToString() + "st";
            else if ((standing.Position % 10) == 2)
                output[17] = standing.Position.ToString() + "nd";
            else if ((standing.Position % 10) == 3)
                output[17] = standing.Position.ToString() + "rd";
            else
                output[17] = standing.Position.ToString() + "th";

            /*if ((DateTime.Now - standing.OffTrackSince).TotalMilliseconds > 1000 && standing.OnTrack == false && SharedData.allowRetire)*/
            if (standing.TrackSurface == SurfaceType.NotInWorld && SharedData.allowRetire)
            {
                output[18] = translation["out"];
            }
            else
            {

                if (standing.Position == 1)
                {
                    if (session.Type == SessionInfo.sessionType.race)
                        output[18] = translation["leader"];
                    else
                        output[18] = iRTVO.Overlay.floatTime2String(standing.FastestLap, true, false);
                }
                else if (standing.PreviousLap.GapLaps > 0 && session.Type == SessionInfo.sessionType.race)
                {
                    output[18] = translation["behind"] + standing.PreviousLap.GapLaps + " ";
                    if (standing.PreviousLap.GapLaps > 1)
                        output[18] += translation["laps"];
                    else
                        output[18] += translation["lap"];
                }
                else/* if (SharedData.standing[SharedData.overlaySession].Length > 0 && SharedData.standing[SharedData.overlaySession][0].fastLap > 0)*/
                {
                    if (session.Type == SessionInfo.sessionType.race)
                        output[18] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.PreviousLap.Gap), true, false);
                    else
                        output[18] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.FastestLap - session.FastestLap), true, false);
                }
            }

            // interval
            if (standing.PreviousLap.Position > 1)
            {
                StandingsItem infront = new StandingsItem();
                infront = session.FindPosition(standing.Position - 1);

                if (session.Type == SessionInfo.sessionType.race)
                {
                    if ((int)infront.PreviousLap.LapNum > (int)standing.PreviousLap.LapNum)
                    {
                        output[21] = translation["behind"] + standing.PreviousLap.GapLaps + " ";
                        if (standing.PreviousLap.GapLaps > 1)
                            output[21] += translation["laps"];
                        else
                            output[21] += translation["lap"];
                    }
                    else
                    {
                        output[21] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.PreviousLap.Gap - infront.PreviousLap.Gap), true, false);
                    }
                }
                else
                {
                    output[21] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.FastestLap - infront.FastestLap), true, false);
                }

                output[22] = translation["behind"] + iRTVO.Overlay.floatTime2String((standing.PreviousLap.Gap - infront.PreviousLap.Gap), true, false);
            }
            else
            {

                if (session.Type == SessionInfo.sessionType.race)
                {
                    output[21] = translation["leader"];
                    output[22] = output[21];
                }
                else
                {
                    output[21] = iRTVO.Overlay.floatTime2String(standing.FastestLap, true, false);
                    output[22] = output[21];
                }
            }

            if (session.Type == SessionInfo.sessionType.race)
            {
                output[23] = translation["behind"] + iRTVO.Overlay.floatTime2String((float)standing.GapLive, true, false);
                output[24] = translation["behind"] + iRTVO.Overlay.floatTime2String((float)standing.IntervalLive, true, false);
            }
            else {
                output[23] = output[18];
                output[24] = output[22];
            }

            if (SharedData.SelectedSectors.Count > 0)
            {
                for(int i = 0; i < Math.Max(standing.CurrentLap.SectorTimes.Count, 2); i++) {
                    if (standing.CurrentLap.SectorTimes.Count > i)
                    {
                        output[27 + i] = standing.CurrentLap.SectorTimes[i].Time.ToString("0.0");
                    }
                }
            }

            string[] extrenal;
            if (SharedData.externalData.ContainsKey(standing.Driver.UserId))
                extrenal = SharedData.externalData[standing.Driver.UserId];
            else
                extrenal = new string[0];

            string[] merged = new string[output.Length + extrenal.Length];
            Array.Copy(output, 0, merged, 0, output.Length);
            Array.Copy(extrenal, 0, merged, output.Length, extrenal.Length);

            return merged;
        }
        public string formatFollowedText(LabelProperties label, StandingsItem standing, SessionInfo session)
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
            };

            /*
            // TODO make faster
            for (int i = 0; i < SharedData.standing[session].Length; i++)
            {
                if (SharedData.standing[session][i].id == driver)
                {
                    position = i;
                    if (label.offset != 0)
                    {
                        position += label.offset;
                        if (position < SharedData.standing[session].Length && position >= 0)
                            driver = SharedData.standing[session][position].id;
                        else // out of bounds
                        {
                            driver = 0;
                            position = -64;
                            output = "";
                        }
                    }
                    break;
                }
            }
            */
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

            try
            {
                //if (position >= 0 && position < session.Standings.Count)
                output = String.Format(format, getFollewedFormats(standing, session));
                //else if (SharedData.standing[session].Length == 0 || position > -64)
                //    output = String.Format(format, getFollewedFormats(SharedData.drivers[driver], new SharedData.LapInfo(), 0));
            }
            catch (FormatException)
            {
                if (MsgBoxShown == false)
                {
                    System.Windows.MessageBox.Show("Invalid formatting:\"" + label.text + "\"");
                    MsgBoxShown = true;
                }
                output = "[invalid]";
                SharedData.runOverlay = false;
            }

            if (label.uppercase)
                return output.ToUpper();
            else
                return output;
        }


        public string[] getSessionstateFormats(SessionInfo session)
        {
            string[] output = new string[15] {
                session.LapsComplete.ToString(),
                session.LapsRemaining.ToString(),
                iRTVO.Overlay.floatTime2String((float)session.SessionLength, false, true),
                iRTVO.Overlay.floatTime2String((float)session.TimeRemaining, false, true),
                "",
                iRTVO.Overlay.floatTime2String((float)session.Time, false, true),
                "",
                SharedData.Track.name,
                Math.Round(SharedData.Track.length * 0.6214 / 1000, 3).ToString(),
                Math.Round(SharedData.Track.length / 1000, 3).ToString(),
                session.Cautions.ToString(),
                session.CautionLaps.ToString(),
                session.LeadChanges.ToString(),
                "",
                (session.LapsComplete + 1).ToString(),
            };

            if ((session.LapsComplete) < 0)
                output[4] = "0";
            else
                output[4] = session.LapsComplete.ToString();

            // lap counter
            if (session.LapsTotal == Int32.MaxValue)
            {
                if (session.State == SessionInfo.sessionState.checkered) // session ending
                    output[6] = translation["finishing"];
                else // normal
                    output[6] = iRTVO.Overlay.floatTime2String((float)SharedData.Sessions.CurrentSession.TimeRemaining, false, true);
            }
            else if (session.State == SessionInfo.sessionState.gridding)
            {
                output[6] = translation["gridding"];
            }
            else if (session.State == SessionInfo.sessionState.pacing)
            {
                output[6] = translation["pacelap"];
            }
            else
            {
                int currentlap = session.LapsComplete;

                if (session.LapsRemaining < 1)
                {
                    output[6] = translation["finishing"];
                }
                else if (session.LapsRemaining == 1)
                {
                    output[6] = translation["finallap"];
                }
                else if (session.LapsRemaining <= Properties.Settings.Default.countdownThreshold)
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
                        (currentlap+1),
                        translation["of"],
                        session.LapsTotal
                    );

                }
            }

            switch (session.Type)
            {
                case SessionInfo.sessionType.race:
                    output[13] = translation["race"];
                    break;
                case SessionInfo.sessionType.qualify:
                    output[13] = translation["qualify"];
                    break;
                case SessionInfo.sessionType.practice:
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
            };

            StringBuilder t = new StringBuilder(label.text);

            foreach (KeyValuePair<string, int> pair in formatMap)
            {
                t.Replace("{" + pair.Key + "}", "{" + pair.Value + "}");
            }

            if (label.uppercase)
                return String.Format(t.ToString(), getSessionstateFormats(SharedData.Sessions.SessionList[session])).ToUpper();
            else
                return String.Format(t.ToString(), getSessionstateFormats(SharedData.Sessions.SessionList[session]));
        }

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
                    string filename = Directory.GetCurrentDirectory() + "\\themes\\" + this.name + "\\cars.ini";
                    if (!File.Exists(filename))
                        filename = Directory.GetCurrentDirectory() + "\\cars.ini";

                    if (File.Exists(filename))
                    {
                        IniFile carNames;

                        carNames = new IniFile(filename);
                        string name = carNames.IniReadValue("Multiclass", car);

                        if (name.Length > 0)
                        {
                            carClass.Add(car, name);
                            //SharedData.webUpdateWait[(int)webTiming.postTypes.cars] = true;
                            return name;
                        }
                        else
                        {
                            carClass.Add(car, car);
                            //SharedData.webUpdateWait[(int)webTiming.postTypes.cars] = true;
                            return car;
                        }
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

        private string getCar(string car)
        {
            if (car != null)
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
                        string name = carNames.IniReadValue("Cars", car);

                        if (name.Length > 0)
                        {
                            carName.Add(car, name);
                            //SharedData.webUpdateWait[(int)webTiming.postTypes.cars] = true;
                            return name;
                        }
                        else
                        {
                            carName.Add(car, car);
                            //SharedData.webUpdateWait[(int)webTiming.postTypes.cars] = true;
                            return car;
                        }
                    }
                    else
                    {
                        carName.Add(car, car);
                        return car;
                    }
                }
            }
            else
                return "";
        }

        public void readExternalData()
        {
            SharedData.externalData.Clear();

            string filename = Directory.GetCurrentDirectory() + "\\themes\\" + this.name + "\\data.csv";
            if (File.Exists(filename))
            {
                string[] lines = System.IO.File.ReadAllLines(filename);

                foreach (string line in lines)
                {
                    string[] split = line.Split(';');
                    int custId = Int32.Parse(split[0]);
                    string[] data = new string[split.Length-1];

                    Array.Copy(split, 1, data, 0, data.Length);
                    SharedData.externalData.Add(custId, data);
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
    }
}
