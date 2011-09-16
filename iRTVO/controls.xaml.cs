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

            if (Properties.Settings.Default.AoTcontrols == true)
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
            //Set the window style to noactivate.
            WindowInteropHelper helper = new WindowInteropHelper(this);
            SetWindowLong(helper.Handle, GWL_EXSTYLE, GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
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
            updateTimer.Interval = new TimeSpan(0, 0, 0, 1);
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

                foreach (DriverInfo driver in SharedData.Drivers)
                {
                    if (driver.Name != "Pace Car" && driver.CarIdx < 63)
                    {
                        cboxitem = new ComboBoxItem();
                        cboxitem.Content = driver.NumberPlate + " " + driver.Name;
                        driverSelect.Items.Add(cboxitem);
                        if(driver.CarIdx == SharedData.Sessions.CurrentSession.FollowedDriver.Driver.CarIdx)
                            driverSelect.SelectedItem = cboxitem;
                    }
                }

                cboxitem = new ComboBoxItem();
                cboxitem.Content = "Most exiting";
                driverSelect.Items.Add(cboxitem);

                cboxitem = new ComboBoxItem();
                cboxitem.Content = "Leader";
                driverSelect.Items.Add(cboxitem);

                cboxitem = new ComboBoxItem();
                cboxitem.Content = "Crashes";
                driverSelect.Items.Add(cboxitem);

                SharedData.updateControls = false;
            }

            if (API.sdk.IsConnected() && API.sdk.GetData("ReplayPlaySpeed") != null)
            {
                Int32 playspeed = (Int32)API.sdk.GetData("ReplayPlaySpeed");
                if (playspeed > 0)
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
                    driver = Int32.Parse(split[1]);
                }

                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, driver, camera);
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
                if (playspeed > 0)
                {
                    API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 0, 0);
                    playButton.Content = "4";
                }
                else
                {
                    API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
                    playButton.Content = ";";
                }
            }
        }

        private void liveButton_Click(object sender, RoutedEventArgs e)
        {
            if (API.sdk.IsConnected())
            {
                /*
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySearch, (int)iRSDKSharp.ReplaySearchModeTypes.ToEnd, 0);
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
                 * */
                replayThread = new Thread(live);
                replayThread.Start();
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

            SharedData.replayInProgress = true;
            SharedData.replayReady.Reset();
            SharedData.replayReady.WaitOne();

            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 0, 0);

            Thread.Sleep(Properties.Settings.Default.ReplayMinLength - 200);

            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, Int32.Parse(ev.Driver.NumberPlate), -1);

            int replayposhigh = (int)(ev.ReplayPos >> 32);
            int replayposlow = (int)(ev.ReplayPos & 0x7FFFFFFF);

            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlayPosition, replayposhigh, replayposlow);
            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);

            SharedData.updateControls = true;

            SharedData.replayInProgress = false;
        }

        public void live()
        {

            SharedData.replayInProgress = true;
            SharedData.replayReady.Reset();
            SharedData.replayReady.WaitOne();

            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 0, 0);

            Thread.Sleep(Properties.Settings.Default.ReplayMinLength - 200);

            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySearch, (int)iRSDKSharp.ReplaySearchModeTypes.ToEnd, 0);
            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);

            SharedData.updateControls = true;

            SharedData.replayInProgress = false;
        }

        private void instantReplay_Click(object sender, RoutedEventArgs e)
        {
            ComboBoxItem cbi = (ComboBoxItem)Rewind.SelectedItem;
            string secstr = cbi.Content.ToString();
            int secint = Int32.Parse(secstr.Substring(0, secstr.Length - 1));

            Event ev = new Event(
                Event.eventType.bookmark,
                (Int32)API.sdk.GetData("ReplayFrameNum") - (secint * 60),
                SharedData.Sessions.CurrentSession.FollowedDriver.Driver,
                "",
                Sessions.SessionInfo.sessionType.invalid,
                0);

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
    }
}
