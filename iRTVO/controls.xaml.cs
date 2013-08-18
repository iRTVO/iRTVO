using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Windows.Threading;
using System.Globalization;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace iRTVO
{
    /// <summary>
    /// Interaction logic for controls.xaml
    /// </summary>
    public partial class Controls : Window
    {
        iRacingAPI API;
        DateTime cameraUpdate = DateTime.Now;
        DispatcherTimer updateTimer = new DispatcherTimer();
        Boolean autoCommitEnabled = false;
        Thread replayThread;

        public Controls()
        {
            InitializeComponent();

            this.Left = Properties.Settings.Default.controlsWindowLocationX;
            this.Top = Properties.Settings.Default.controlsWindowLocationY;

            if (SharedData.settings.AlwaysOnTopCameraControls)
                this.Topmost = true;
            else
                this.Topmost = false;

            API = new iRTVO.iRacingAPI();
            API.sdk.Startup();
        }

        // no focus
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (SharedData.settings.LoseFocus)
            {
                //Set the window style to noactivate.
                WindowInteropHelper helper = new WindowInteropHelper(this);
                SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
            }
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private void controlsWindow_LocationChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.controlsWindowLocationX = (int)this.Left;
            Properties.Settings.Default.controlsWindowLocationY = (int)this.Top;
            Properties.Settings.Default.Save();
        }

        private void controlsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            updateTimer.Interval = new TimeSpan(0, 0, 0, 0, 250);
            updateTimer.Tick += new EventHandler(updateControls);
            updateTimer.Start();
            cameraUpdate = DateTime.MinValue;
            cameraSelectComboBox.Items.Clear();
            driverSelect.Items.Clear();
            updateControls(new object(), new EventArgs());
            
        }

        private void updateControls(object sender, EventArgs e)
        {

            if (SharedData.Camera.Updated > cameraUpdate)
            {
                if (SharedData.Camera.Groups.Count > 0)
                {
                    cameraSelectComboBox.Items.Clear();
                    ComboBoxItem cboxitem;

                    foreach (CameraInfo.CameraGroup cam in SharedData.Camera.Groups)
                    {
                        cboxitem = new ComboBoxItem();
                        cboxitem.Content = cam.Id + " " + cam.Name;
                        cameraSelectComboBox.Items.Add(cboxitem);
                        if (cam.Id == SharedData.Camera.CurrentGroup)
                            cameraSelectComboBox.SelectedItem = cboxitem;
                    }

                    cameraUpdate = DateTime.Now;
                }
            }


            if ((SharedData.Drivers.Count - 2) > driverSelect.Items.Count || 
                (driverSelect.SelectedItem != null && SharedData.updateControls))
            {
                driverSelect.Items.Clear();
                ComboBoxItem cboxitem;

                IEnumerable<DriverInfo> dQuery;

                if(SharedData.settings.CameraControlSortByNumber)
                    dQuery = SharedData.Drivers.OrderBy(s => s.NumberPlateInt);
                else
                    dQuery = SharedData.Drivers.OrderBy(s => s.Name);    

                if (SharedData.settings.CameraControlIncludeSaferyCar)
                {
                    cboxitem = new ComboBoxItem();
                    cboxitem.Content = "0 Safety Car";
                    driverSelect.Items.Add(cboxitem);
                    if (SharedData.Sessions.CurrentSession.FollowedDriver.Driver.CarIdx == -1)
                        driverSelect.SelectedItem = cboxitem;
                }

                foreach (DriverInfo driver in dQuery)
                {
                    if (driver.CarIdx < 63)
                    {
                        cboxitem = new ComboBoxItem();
                        cboxitem.Content = driver.NumberPlate + " " + driver.Name;
                        driverSelect.Items.Add(cboxitem);
                        if (driver.CarIdx == SharedData.Sessions.CurrentSession.FollowedDriver.Driver.CarIdx)
                            driverSelect.SelectedItem = cboxitem;
                    }
                }

                // ir only
                if (API.sdk.IsConnected())
                {
                    cboxitem = new ComboBoxItem();
                    cboxitem.Content = "Most exiting";
                    driverSelect.Items.Add(cboxitem);

                    cboxitem = new ComboBoxItem();
                    cboxitem.Content = "Leader";
                    driverSelect.Items.Add(cboxitem);

                    cboxitem = new ComboBoxItem();
                    cboxitem.Content = "Crashes";
                    driverSelect.Items.Add(cboxitem);
                }

                SharedData.updateControls = false;
            }

            if (API.sdk.IsConnected() && API.sdk.GetData("ReplayPlaySpeed") != null)
            {
                Int32 playspeed = (Int32)API.sdk.GetData("ReplayPlaySpeed");
                if (playspeed != 1)
                {
                    playButton.Content = "4";
                }
                else
                {
                    playButton.Content = ";";
                }
            }

        }

        private void autoCommit(object sender, SelectionChangedEventArgs e)
        {
            if (autoCommitEnabled)
                commit();
        }

        private void commit()
        {
            if (driverSelect.SelectedItem != null && cameraSelectComboBox.SelectedItem != null)
            {
                String[] split = cameraSelectComboBox.SelectedItem.ToString().Split(' ');
                int camera = Int32.Parse(split[1]);
                split = driverSelect.SelectedItem.ToString().Split(' ');
                int driver = 0;

                if (split[1] == "Most")
                {
                    driver = -1;
                }
                else if (split[1] == "Leader")
                {
                    driver = -2;
                }
                else if (split[1] == "Crashes")
                {
                    driver = -3;
                }
                else
                {
                    driver = padCarNum(split[1]);
                }

                if (API.sdk.IsConnected())
                {
                    API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, driver, camera);
                    Int32 playspeed = getPlaySpeed();
                    Int32 slomo = 0;
                    if (playspeed > 0)
                        slomo = 1;
                    else
                        playspeed = Math.Abs(playspeed);
                    API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, playspeed, slomo);
                }
                else if (SharedData.rfAPI.initialized)
                {
                    SharedData.rfAPI.setCamera(driver, camera);
                }

                if (SharedData.remoteClient != null)
                {
                    SharedData.remoteClient.sendMessage("CAMERA;" + camera);
                    SharedData.remoteClient.sendMessage("DRIVER;" + driver);
                    SharedData.remoteClient.sendMessage("PLAYSPEED;" + ((Int32)API.sdk.GetData("ReplayPlaySpeed")).ToString()+ "-"+ ((Int32)API.sdk.GetData("ReplayPlaySlowMotion")).ToString());

                }
                else if (SharedData.serverThread.IsAlive)
                {
                    SharedData.serverOutBuffer.Push("CAMERA;" + camera);
                    SharedData.serverOutBuffer.Push("DRIVER;" + driver);
                    SharedData.remoteClient.sendMessage("PLAYSPEED;" + ((Int32)API.sdk.GetData("ReplayPlaySpeed")).ToString() + "-" + ((Int32)API.sdk.GetData("ReplayPlaySlowMotion")).ToString());
                }
            }
        }

        private Int32 getPlaySpeed()
        {
            ComboBoxItem selected = (ComboBoxItem)PlaySpeed.SelectedItem;
            switch (selected.Content.ToString())
            {
               
                case "1/2x":
                    return 1;
                case "1/3x":
                    return 2;
                case "1/4x":
                    return 3;
                case "1/5x":
                    return 4;
                case "1/6x":
                    return 5;
                case "1/7x":
                    return 6;
                case "1/8x":
                    return 7;
                case "1/9x":
                    return 8;
                case "1/10x":
                    return 9;
                case "1/11x":
                    return 10;
                case "1/12x":
                    return 11;
                case "1/13x":
                    return 12;
                case "1/14x":
                    return 13;
                case "1/15x":
                    return 14;
                case "1/16x":
                    return 15;
                default:
                    return -1;
            }

        }

        private void commitButton_Click(object sender, RoutedEventArgs e)
        {
            commit();
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if (API.sdk.IsConnected())
            {
                Int32 playspeed = (Int32)API.sdk.GetData("ReplayPlaySpeed");
                if (playspeed != 1)
                {
                    if (SharedData.remoteClient != null)
                        SharedData.remoteClient.sendMessage("PLAY;");
                    else if (SharedData.serverThread.IsAlive)
                        SharedData.serverOutBuffer.Push("PLAY;");

                    API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
                    playButton.Content = "4";
                }
                else
                {
                    if (SharedData.remoteClient != null)
                        SharedData.remoteClient.sendMessage("PAUSE;");
                    else if (SharedData.serverThread.IsAlive)
                        SharedData.serverOutBuffer.Push("PAUSE;");

                    API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 0, 0);
                    playButton.Content = ";";
                }
            }
        }

        private void liveButton_Click(object sender, RoutedEventArgs e)
        {
            if (API.sdk.IsConnected())
            {
                replayThread = new Thread(live);
                replayThread.Start();
                SharedData.triggers.Push(TriggerTypes.live);
            }
        }

        private void addBookmark_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.Sessions.CurrentSession.FollowedDriver.Driver.NumberPlate.Length > 0)
            {
                Event ev = new Event(Event.eventType.fastlap, (Int32)API.sdk.GetData("ReplayFrameNum"), SharedData.Sessions.CurrentSession.FollowedDriver.Driver, "Manual bookmark", SharedData.Sessions.CurrentSession.Type, SharedData.Sessions.CurrentSession.FollowedDriver.CurrentLap.LapNum);
                SharedData.Bookmarks.List.Add(ev);
            }
        }

        private void beginButton_Click(object sender, RoutedEventArgs e)
        {
            Event prevEvent;
            int diff = 0;

            if(SharedData.Bookmarks.List.Count > 0) 
            {
                prevEvent = SharedData.Bookmarks.List[0]; // pick first
                diff = (Int32)API.sdk.GetData("ReplayFrameNum");

                foreach (Event ev in SharedData.Bookmarks.List)
                {
                    if (((Int32)API.sdk.GetData("ReplayFrameNum") - ev.ReplayPos) < diff)
                    {
                        prevEvent = ev;
                    }
                }

                replayThread = new Thread(rewind);
                replayThread.Start(prevEvent);
                /*
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, Int32.Parse(prevEvent.Driver.NumberPlate), -1);
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlayPosition, 0, prevEvent.ReplayPos);
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
                 * */
            }
        }

        private void autoCommitButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)e.Source;

            if (autoCommitEnabled)
            {
                btn.Content = "Auto apply";
                autoCommitEnabled = false;
            }
            else
            {
                btn.Content = "Manual apply";
                autoCommitEnabled = true;
            }
        }

        public void rewind(Object input)
        {
            Event ev = (Event)input;
            Int32 rewindFrames = (Int32)API.sdk.GetData("ReplayFrameNum") - (int)ev.ReplayPos;

            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, padCarNum(ev.Driver.NumberPlate), -1);
            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlayPosition, (int)iRSDKSharp.ReplayPositionModeTypes.Begin, (int)(ev.ReplayPos - (ev.Rewind * 60)));

            Int32 curpos = (Int32)API.sdk.GetData("ReplayFrameNum");
            DateTime timeout = DateTime.Now;

            // wait rewind to finish, but only 15 secs
            while (curpos != (int)(ev.ReplayPos - (ev.Rewind * 60)) && (DateTime.Now - timeout).TotalSeconds < 15)
            {
                Thread.Sleep(16);
                curpos = (Int32)API.sdk.GetData("ReplayFrameNum");
            }

            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);

            SharedData.updateControls = true;

            if (SharedData.remoteClient != null)
                SharedData.remoteClient.sendMessage("REWIND;" + rewindFrames.ToString());
            else if (SharedData.serverThread.IsAlive)
                SharedData.serverOutBuffer.Push("REWIND;" + rewindFrames.ToString());
        }

        public void live()
        {
            SharedData.triggers.Push(TriggerTypes.live);

            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySearch, (int)iRSDKSharp.ReplaySearchModeTypes.ToEnd, 0);
            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
            SharedData.updateControls = true;

            if (SharedData.remoteClient != null)
                SharedData.remoteClient.sendMessage("LIVE;");
            else if (SharedData.serverThread.IsAlive)
                SharedData.serverOutBuffer.Push("LIVE;");
        }

        private void instantReplay_Click(object sender, RoutedEventArgs e)
        {
            SharedData.triggers.Push(TriggerTypes.replay);

            ComboBoxItem cbi = (ComboBoxItem)Rewind.SelectedItem;
            string secstr = cbi.Content.ToString();
            int secint = Int32.Parse(secstr.Substring(0, secstr.Length - 1));

            Event ev = new Event(
                Event.eventType.bookmark,
                (Int32)API.sdk.GetData("ReplayFrameNum") - (secint * 60),
                SharedData.Sessions.CurrentSession.FollowedDriver.Driver,
                "",
                Sessions.SessionInfo.sessionType.invalid,
                0
            );

            replayThread = new Thread(rewind);
            replayThread.Start(ev);
        }

        private void uiCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (uiCheckBox.IsChecked == false)
            {
                SharedData.showSimUi = true;
            }
            else
            {
                SharedData.showSimUi = false;
            }
        }

        public static int padCarNum(string input)
        {
            int num = Int32.Parse(input);
            int zero = input.Length - num.ToString().Length;

            int retVal = num;
            int numPlace = 1;
            if (num > 99)
                numPlace = 3;
            else if (num > 9)
                numPlace = 2;
            if (zero > 0)
            {
                numPlace += zero;
                retVal = num + 1000 * numPlace;
            }

            return retVal;
        }

        private void DriverBrowse_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;

            Int32 curPos = SharedData.Sessions.CurrentSession.FollowedDriver.Position;
            Int32 nextPos = curPos;

            if(btn.Name == "nextDriver")
                nextPos--;
            else if(btn.Name == "prevDriver")
                nextPos++;

            String nextPlate = "";

            if (nextPos < 1)
            {
                nextPlate = SharedData.Sessions.CurrentSession.FindPosition(SharedData.Drivers.Count, dataorder.position).Driver.NumberPlate;
            }
            else if (nextPos > SharedData.Sessions.CurrentSession.Standings.Count)
            {
                nextPlate = SharedData.Sessions.CurrentSession.getLeader().Driver.NumberPlate;
            }
            else
            {
                nextPlate = SharedData.Sessions.CurrentSession.FindPosition(nextPos, dataorder.position).Driver.NumberPlate;
            }

            if (cameraSelectComboBox.SelectedItem != null)
            {
                String[] split = cameraSelectComboBox.SelectedItem.ToString().Split(' ');
                int camera = Int32.Parse(split[1]);

                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, padCarNum(nextPlate), camera);

                foreach (ComboBoxItem cbi in driverSelect.Items)
                {
                    split = cbi.Content.ToString().Split(' ');
                    if (split[0] == nextPlate)
                    {
                        driverSelect.SelectedItem = cbi;
                        break;
                    }
                }
            }
        }
    }
}
