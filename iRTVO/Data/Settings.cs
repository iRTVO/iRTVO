
using iRTVO.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRTVO.Data
{
    public class Settings : ISettings
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

        [CfgSetting(Section="Theme",Entry="Name",DefaultValue="FIA Style")]
        public String Theme = "FIA Style";
        [CfgSetting(Section = "Theme", Entry = "UpdateFPS", DefaultValue = "30")]
        public Int32 UpdateFPS = 30;
        [CfgSetting(Section = "Theme", Entry = "LapCountdownFrom", DefaultValue = "50")]
        public Int32 LapCountdownFrom = 50;
        [CfgSetting(Section = "Theme", Entry = "DeltaDistance", DefaultValue = "10")]
        public Single DeltaDistance = 10;
        [CfgSetting(Section = "Theme", Entry = "IncludeMe", DefaultValue = "False")]
        public Boolean IncludeMe = false;

        [CfgSetting(Section = "MainWindow", Entry = "CamerasButtonColumn", DefaultValue = "False")]
        public Boolean CamerasButtonColumn = false;
        [CfgSetting(Section = "MainWindow", Entry = "CamerasPerColumn", DefaultValue = "10")]
        public Int32 CamerasPerColumn = 100;
        [CfgSetting(Section = "MainWindow", Entry = "IgnoredCameras", DefaultValue = "")]
        public List<string> IgnoredCameras = new List<string>();
        [CfgSetting(Section = "MainWindow", Entry = "X", DefaultValue = "100")]
        public Int32 MainWindowLocationX = 0;
        [CfgSetting(Section = "MainWindow", Entry = "Y", DefaultValue = "100")]
        public Int32 MainWindowLocationY = 0;
        [CfgSetting(Section = "MainWindow", Entry = "W", DefaultValue = "0")]
        public Int32 MainWindowWidth = 0;
        [CfgSetting(Section = "MainWindow", Entry = "H", DefaultValue = "0")]
        public Int32 MainWindowHeight = 0;

        [CfgSetting(Section = "Overlay", Entry = "X", DefaultValue = "0")]
        public Int32 OverlayX = 0;
        [CfgSetting(Section = "Overlay", Entry = "Y", DefaultValue = "0")]
        public Int32 OverlayY = 0;
        [CfgSetting(Section = "Overlay", Entry = "W", DefaultValue = "1280")]
        public Int32 OverlayW = 1280;
        [CfgSetting(Section = "Overlay", Entry = "H", DefaultValue = "720")]
        public Int32 OverlayH = 720;
        [CfgSetting(Section = "Overlay", Entry = "ShowBorders", DefaultValue = "False")]
        public Boolean OverlayShowBorders = false;


        [CfgSetting(Section = "remote control server", Entry = "Password", DefaultValue = "")]
        public String RemoteControlServerPassword = "";
        [CfgSetting(Section = "remote control server", Entry = "Port", DefaultValue = "10700")]
        public Int32 RemoteControlServerPort = 10700;
        [CfgSetting(Section = "remote control server", Entry = "AutoStart", DefaultValue = "False")]
        public Boolean RemoteControlServerAutostart = false;

        [CfgSetting(Section = "remote control client", Entry = "Password", DefaultValue = "")]
        public String RemoteControlClientPassword = "";
        [CfgSetting(Section = "remote control client", Entry = "Address", DefaultValue = "")]
        public String RemoteControlClientAddress = "";
        [CfgSetting(Section = "remote control client", Entry = "Port", DefaultValue = "10700")]
        public Int32 RemoteControlClientPort = 10700;
        [CfgSetting(Section = "remote control client", Entry = "AutoStart", DefaultValue = "False")]
        public Boolean RemoteControlClientAutostart = false;

        [CfgSetting(Section = "WebTiming", Entry = "Password", DefaultValue = "")]
        public String WebTimingPassword = "";
        [CfgSetting(Section = "WebTiming", Entry = "URL", DefaultValue = "")]
        public String WebTimingUrl = "";
        [CfgSetting(Section = "WebTiming", Entry = "Interval", DefaultValue = "10")]
        public Int32 WebTimingUpdateInterval = 10;
        [CfgSetting(Section = "WebTiming", Entry = "Enable", DefaultValue = "False")]
        public Boolean WebTimingEnable = false;

        [CfgSetting(Section = "Windows", Entry = "AlwaysOnTopMainWindow", DefaultValue = "False")]
        public Boolean AlwaysOnTopMainWindow = false;
        [CfgSetting(Section = "Windows", Entry = "AlwaysOnTopCameraControls", DefaultValue = "False")]
        public Boolean AlwaysOnTopCameraControls = false;
        [CfgSetting(Section = "Windows", Entry = "AlwaysOnTopLists", DefaultValue = "False")]
        public Boolean AlwaysOnTopLists = false;
        [CfgSetting(Section = "Windows", Entry = "LoseFocus", DefaultValue = "False")]
        public Boolean LoseFocus = false;

        [CfgSetting(Section = "Controls", Entry = "SortByNumber", DefaultValue = "False")]
        public Boolean CameraControlSortByNumber = false;
        [CfgSetting(Section = "Controls", Entry = "SafetyCar", DefaultValue = "False")]
        public Boolean CameraControlIncludeSafetyCar = false;
        [CfgSetting(Section = "Controls", Entry = "X", DefaultValue = "100")]
        public Int32 ControlsWindowLocationX = 0;
        [CfgSetting(Section = "Controls", Entry = "Y", DefaultValue = "100")]
        public Int32 ControlsWindowLocationY = 0;

        [CfgSetting(Section = "ListsWindow", Entry = "X", DefaultValue = "0")]
        public Int32 ListsWindowLocationX = 0;
        [CfgSetting(Section = "ListsWindow", Entry = "Y", DefaultValue = "0")]
        public Int32 ListsWindowLocationY = 0;
        [CfgSetting(Section = "ListsWindow", Entry = "W", DefaultValue = "640")]
        public Int32 ListsWindowWidth = 0;
        [CfgSetting(Section = "ListsWindow", Entry = "H", DefaultValue = "468")]
        public Int32 ListsWindowHeight = 0;


        [CfgSetting(Section = "Simulation", Entry = "API", DefaultValue = "iRacing")]
        public string SimulationApiName = "iRacing";
        [CfgSetting(Section = "Simulation", Entry = "ConnectDelay", DefaultValue = "30")]
        public int SimulationConnectDelay = 30;
        
        [CfgSetting(Section = "DriversWindow", Entry = "DriversColumns", DefaultValue = "")]
        public List<string> _DriversColumns = new List<string>();
        // Not exported. But Synced with above
        public List<ColumnSetting> DriversColumns = new List<ColumnSetting>();

        private CfgFile configFile = null;

        
        public Settings(String filename)
        {
            configFile = new CfgFile(filename);
            Load();
        }

        public void Load()
        {
            configFile.Load();
            configFile.Deserialize(this);

            // Cleanup Values
            if ( !String.IsNullOrEmpty( configFile.getValue("Controls","SaferyCar",false,String.Empty,false) ))
            {
                CameraControlIncludeSafetyCar = Boolean.Parse(configFile.getValue("Controls", "SaferyCar", false, String.Empty, false));
                configFile.deleteValue("Controls", "SaferyCar", false);
            }

            // Countercheck Values
            if (this.DeltaDistance < 0.5)
                this.DeltaDistance = 10;

            if (SimulationConnectDelay < 3)
                SimulationConnectDelay = 3;

            if (_DriversColumns.Count > 0)
            {
                foreach (string value in _DriversColumns)
                {
                    string[] parts = value.Split(':');

                    if ((parts.Length > 1) && !String.IsNullOrEmpty(parts[1]))
                        this.DriversColumns.Add(new ColumnSetting { Name = parts[0].Trim(), Header = parts[1].Trim() });
                    else
                        this.DriversColumns.Add(new ColumnSetting { Name = parts[0].Trim(), Header = parts[0].Trim() });
                }

            }
            Save();
        }

        public void Save()
        {
            configFile.Serialize(this);
            configFile.Save();
        }

        public String getValue(String Section, String Entry, Boolean CaseSensitive, String defaultValue, bool add)
        {
            return configFile.getValue(Section, Entry, CaseSensitive, defaultValue, add);
        }

        public Boolean setValue(String Section, String Entry, String Value, Boolean CaseSensitive)
        {
            return configFile.setValue(Section, Entry, Value, CaseSensitive);
        }
    }
}
