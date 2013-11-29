using Ini;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class Settings
    {
        public class ColumnSetting
        {
            public string Name;
            public string Header;

            public override string ToString()
            {
                return String.Format("{0}:{1}", Name, Header);
            }
        }

        public String Theme = "FIA Style";
        public Int32 UpdateFPS = 30;
        public Int32 LapCountdownFrom = 50;
        public Single DeltaDistance = 10;
        public Boolean IncludeMe = false;
        public Boolean CamButtonRow = false;
        public Int32 CamsPerRow = 100;
        public List<string> CamButtonIgnore = new List<string>();

        public Int32 OverlayX = 0;
        public Int32 OverlayY = 0;
        public Int32 OverlayW = 1280;
        public Int32 OverlayH = 720;

        public Int32 RemoteControlServerPort = 10700;
        public String RemoteControlServerPassword = "";
        public Boolean RemoteControlServerAutostart = false;

        public Int32 RemoteControlClientPort = 10700;
        public String RemoteControlClientAddress = "";
        public String RemoteControlClientPassword = "";
        public Boolean RemoteControlClientAutostart = false;

        public String WebTimingUrl = "";
        public String WebTimingPassword = "";
        public Int32 WebTimingUpdateInterval = 10;
        public Boolean WebTimingEnable = false;

        public Boolean AlwaysOnTopMainWindow = false;
        public Boolean AlwaysOnTopCameraControls = false;
        public Boolean AlwaysOnTopLists = false;
        public Boolean LoseFocus = false;

        public Boolean CameraControlSortByNumber = false;
        public Boolean CameraControlIncludeSaferyCar = false;

        public string SimulationApiName = "iRacing";
        public int SimulationConnectDelay = 30;

        public List<ColumnSetting> StandingsGridAdditionalColumns = new List<ColumnSetting>();

        public Settings(String filename)
        {
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            IniFile ini = new IniFile(filename);


            if (ini.isNew)
            {
                // Default Configuration   

                ini.SetValue("theme", "name", Properties.Settings.Default.theme);
                ini.SetValue("theme", "updatefps", Properties.Settings.Default.UpdateFrequency.ToString());
                ini.SetValue("theme", "lapcountdownfrom", Properties.Settings.Default.countdownThreshold.ToString());

                ini.SetValue("overlay", "x", Properties.Settings.Default.OverlayLocationX.ToString());
                ini.SetValue("overlay", "y", Properties.Settings.Default.OverlayLocationY.ToString());
                ini.SetValue("overlay", "w", Properties.Settings.Default.OverlayWidth.ToString());
                ini.SetValue("overlay", "h", Properties.Settings.Default.OverlayHeight.ToString());

                ini.SetValue("remote control server", "password", Properties.Settings.Default.remoteServerKey);
                ini.SetValue("remote control server", "port", Properties.Settings.Default.remoteServerPort.ToString());
                ini.SetValue("remote control server", "autostart", Properties.Settings.Default.remoteServerAutostart.ToString().ToLower());

                ini.SetValue("remote control client", "password", Properties.Settings.Default.remoteClientKey);
                ini.SetValue("remote control client", "port", Properties.Settings.Default.remoteClientPort.ToString());
                ini.SetValue("remote control client", "address", Properties.Settings.Default.remoteClientIp);
                ini.SetValue("remote control client", "autostart", Properties.Settings.Default.remoteClientAutostart.ToString().ToLower());

                ini.SetValue("webtiming", "password", Properties.Settings.Default.webTimingKey);
                ini.SetValue("webtiming", "url", Properties.Settings.Default.webTimingUrl);
                ini.SetValue("webtiming", "interval", Properties.Settings.Default.webTimingInterval.ToString());
                ini.SetValue("webtiming", "enable", Properties.Settings.Default.webTimingEnable.ToString().ToLower());

                ini.SetValue("windows", "AlwaysOnTopMainWindow", Properties.Settings.Default.AoTmain.ToString().ToLower());
                ini.SetValue("windows", "AlwaysOnTopCameraControls", Properties.Settings.Default.AoTcontrols.ToString().ToLower());
                ini.SetValue("windows", "AlwaysOnTopLists", Properties.Settings.Default.AoTlists.ToString().ToLower());

                ini.SetValue("controls", "sortbynumber", Properties.Settings.Default.DriverListSortNumber.ToString().ToLower());
                ini.SetValue("controls", "saferycar", Properties.Settings.Default.DriverListIncSC.ToString().ToLower());

                ini.SetValue("standingsgrid", "columns", "");
            }

            this.Theme = ini.GetValue("theme", "name");
            this.UpdateFPS = Int32.Parse(ini.GetValue("theme", "updatefps"));
            this.LapCountdownFrom = Int32.Parse(ini.GetValue("theme", "lapcountdownfrom"));

            Single.TryParse(ini.GetValue("theme", "deltadistance"), NumberStyles.AllowDecimalPoint, culture, out this.DeltaDistance);
            if (this.DeltaDistance < 0.5)
                this.DeltaDistance = 10;

            if (ini.GetValue("theme", "includeme").ToLower() == "true")
                this.IncludeMe = true;
            if (ini.HasValue("theme", "cambuttonrow"))
            {
                CamButtonRow = ini.GetValue("theme", "cambuttonrow").ToLower() == "true";
                if (ini.HasValue("theme", "camsperrow"))
                    CamsPerRow = Int32.Parse(ini.GetValue("theme", "camsperrow"));
                if (ini.HasKey("theme", "camsnobutton"))
                    CamButtonIgnore.AddRange(ini.GetValue("theme", "camsnobutton").ToUpper().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }

            this.OverlayX = Int32.Parse(ini.GetValue("overlay", "x"));
            this.OverlayY = Int32.Parse(ini.GetValue("overlay", "y"));
            this.OverlayW = Int32.Parse(ini.GetValue("overlay", "w"));
            this.OverlayH = Int32.Parse(ini.GetValue("overlay", "h"));

            this.RemoteControlServerPassword = ini.GetValue("remote control server", "password");
            this.RemoteControlServerPort = Int32.Parse(ini.GetValue("remote control server", "port"));
            if (ini.GetValue("remote control server", "autostart").ToLower() == "true")
                this.RemoteControlServerAutostart = true;

            this.RemoteControlClientPassword = ini.GetValue("remote control client", "password");
            this.RemoteControlClientPort = Int32.Parse(ini.GetValue("remote control client", "port"));
            this.RemoteControlClientAddress = ini.GetValue("remote control client", "address");
            if (ini.GetValue("remote control client", "autostart").ToLower() == "true")
                this.RemoteControlClientAutostart = true;

            this.WebTimingPassword = ini.GetValue("webtiming", "password");
            this.WebTimingUrl = ini.GetValue("webtiming", "url");
            this.WebTimingUpdateInterval = Int32.Parse(ini.GetValue("webtiming", "interval"));
            if (ini.GetValue("webtiming", "enable").ToLower() == "true")
                this.WebTimingEnable = true;

            if (ini.GetValue("windows", "AlwaysOnTopMainWindow").ToLower() == "true")
                this.AlwaysOnTopMainWindow = true;
            if (ini.GetValue("windows", "AlwaysOnTopCameraControls").ToLower() == "true")
                this.AlwaysOnTopCameraControls = true;
            if (ini.GetValue("windows", "AlwaysOnTopLists").ToLower() == "true")
                this.AlwaysOnTopLists = true;
            if (ini.GetValue("windows", "LoseFocus").ToLower() == "true")
                this.LoseFocus = true;

            if (ini.GetValue("controls", "sortbynumber").ToLower() == "true")
                this.CameraControlSortByNumber = true;
            if (ini.GetValue("controls", "saferycar").ToLower() == "true")
                this.CameraControlIncludeSaferyCar = true;

            if (ini.HasValue("simulation", "api"))
                this.SimulationApiName = ini.GetValue("simulation", "api");
            if (ini.HasValue("simulation", "connectdelay"))
                this.SimulationConnectDelay = Math.Max(Int32.Parse(ini.GetValue("simulation", "connectdelay")), 5); // Minimum delay 5 Seconds

            if (ini.HasValue("standingsgrid", "columns"))
            {
                string[] values = ini.GetValue("standingsgrid", "columns").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string value in values)
                {
                    string[] parts = value.Split(':');

                    if ((parts.Length > 1) && !String.IsNullOrEmpty(parts[1]))
                        this.StandingsGridAdditionalColumns.Add(new ColumnSetting { Name = parts[0].Trim(), Header = parts[1].Trim() });
                    else
                        this.StandingsGridAdditionalColumns.Add(new ColumnSetting { Name = parts[0].Trim(), Header = parts[0].Trim() });
                }

            }




            // update Configuration

            ini.SetValue("theme", "name", this.Theme);
            ini.SetValue("theme", "updatefps", this.UpdateFPS.ToString());
            ini.SetValue("theme", "lapcountdownfrom", this.LapCountdownFrom.ToString());
            ini.SetValue("theme", "deltadistance", this.DeltaDistance.ToString("F5", culture));
            ini.SetValue("theme", "includeme", this.IncludeMe.ToString().ToLower());
            ini.SetValue("theme", "cambuttonrow", CamButtonRow.ToString().ToLower(), "Buttonrow to show Cams in. -1 for hiddden");
            ini.SetValue("theme", "camsperrow", CamsPerRow.ToString());
            ini.SetValue("theme", "camsnobutton", String.Join(",", CamButtonIgnore));

            ini.SetValue("overlay", "x", this.OverlayX.ToString());
            ini.SetValue("overlay", "y", this.OverlayY.ToString());
            ini.SetValue("overlay", "w", this.OverlayW.ToString());
            ini.SetValue("overlay", "h", this.OverlayH.ToString());

            ini.SetValue("remote control server", "password", this.RemoteControlServerPassword);
            ini.SetValue("remote control server", "port", this.RemoteControlServerPort.ToString());
            ini.SetValue("remote control server", "autostart", this.RemoteControlServerAutostart.ToString().ToLower());

            ini.SetValue("remote control client", "password", this.RemoteControlClientPassword);
            ini.SetValue("remote control client", "port", this.RemoteControlClientPort.ToString());
            ini.SetValue("remote control client", "address", this.RemoteControlClientAddress);
            ini.SetValue("remote control client", "autostart", this.RemoteControlClientAutostart.ToString().ToLower());

            ini.SetValue("webtiming", "password", this.WebTimingPassword);
            ini.SetValue("webtiming", "url", this.WebTimingUrl);
            ini.SetValue("webtiming", "interval", this.WebTimingUpdateInterval.ToString());
            ini.SetValue("webtiming", "enable", this.WebTimingEnable.ToString().ToLower());

            ini.SetValue("windows", "AlwaysOnTopMainWindow", this.AlwaysOnTopMainWindow.ToString().ToLower());
            ini.SetValue("windows", "AlwaysOnTopCameraControls", this.AlwaysOnTopCameraControls.ToString().ToLower());
            ini.SetValue("windows", "AlwaysOnTopLists", this.AlwaysOnTopLists.ToString().ToLower());
            ini.SetValue("windows", "LoseFocus", this.LoseFocus.ToString().ToLower());

            ini.SetValue("controls", "sortbynumber", this.CameraControlSortByNumber.ToString().ToLower());
            ini.SetValue("controls", "saferycar", this.CameraControlIncludeSaferyCar.ToString().ToLower());

            ini.SetValue("simulation", "api", this.SimulationApiName);
            ini.SetValue("simulation", "connectdelay", this.SimulationConnectDelay.ToString());

            ini.SetValue("standingsgrid", "columns", String.Join(",", this.StandingsGridAdditionalColumns));

            ini.SaveIniFile();

        }
    }
}
