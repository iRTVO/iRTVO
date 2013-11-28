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
using iRTVO.Networking;
using NLog;
using iRTVO.Interfaces;

namespace iRTVO
{
    /// <summary>
    /// Interaction logic for controls.xaml
    /// </summary>
    public partial class Controls : Window
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        internal ISimulationAPI simulationAPI;
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
            
        }

        public Controls(ISimulationAPI sim) : this()
        {
            simulationAPI = sim;
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
            SharedData.Camera.PropertyChanged += Camera_PropertyChanged;
            SharedData.PropertyChanged += SharedData_PropertyChanged;

        }

        void SharedData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FollowedDriver")
            {
                if (!iRTVOConnection.isConnected || (iRTVOConnection.isConnected && (SharedData.remoteClientFollow || iRTVOConnection.isServer)))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        bool oldCommit = autoCommitEnabled;
                        autoCommitEnabled = false;
                        logger.Trace("New Driver " + SharedData.Sessions.CurrentSession.FollowedDriver.Driver.NumberPlatePadded);
                        driverSelect.SelectedValue = SharedData.Sessions.CurrentSession.FollowedDriver.Driver.NumberPlatePadded;
                        autoCommitEnabled = oldCommit;
                    }));
                }
            }
        }

        void Camera_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentGroup")
            {
                if (!iRTVOConnection.isConnected || (iRTVOConnection.isConnected && (SharedData.remoteClientFollow || iRTVOConnection.isServer)))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        bool oldCommit = autoCommitEnabled;
                        autoCommitEnabled = false;
                        cameraSelectComboBox.SelectedValue = SharedData.Camera.CurrentGroup;
                        autoCommitEnabled = oldCommit;
                    }));
                }

            }
        }

        private void updateControls(object sender, EventArgs e)
        {
            // prevent playing Pingpong with the controls Window, in case one of Theme autoselects is active
            bool oldAutoCommit = autoCommitEnabled;
            autoCommitEnabled = false;

            if ((SharedData.Camera.Updated > cameraUpdate) || SharedData.updateControls)
            {
                if (SharedData.Camera.Groups.Count > 0)
                {
                    cameraSelectComboBox.Items.Clear();
                    ComboBoxItem cboxitem;

                    foreach (CameraInfo.CameraGroup cam in SharedData.Camera.Groups)
                    {
                        cboxitem = new ComboBoxItem();
                        cboxitem.Content = cam.Name;
                        cboxitem.Tag = cam.Id;
                        cameraSelectComboBox.Items.Add(cboxitem);
                        if (cam.Id == SharedData.Camera.CurrentGroup)
                            cameraSelectComboBox.SelectedItem = cboxitem;
                    }

                    cameraUpdate = DateTime.Now;
                }
            }

            // Calculate howmany real drivers we have in the grid
            int numDriverItems = driverSelect.Items.Count - 3 - (SharedData.settings.CameraControlIncludeSaferyCar ? 1 : 0);
            if ((SharedData.Drivers.Count != numDriverItems) || SharedData.updateControls)
            {
                driverSelect.Items.Clear();
                ComboBoxItem cboxitem;

                IEnumerable<DriverInfo> dQuery;

                if (SharedData.settings.CameraControlSortByNumber)
                    dQuery = SharedData.Drivers.OrderBy(s => s.NumberPlateInt);
                else
                    dQuery = SharedData.Drivers.OrderBy(s => s.Name);

                if (SharedData.settings.CameraControlIncludeSaferyCar)
                {
                    cboxitem = new ComboBoxItem();
                    cboxitem.Content = "Safety Car";
                    cboxitem.Tag = 0;
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
                        cboxitem.Tag = padCarNum(driver.NumberPlate);
                        driverSelect.Items.Add(cboxitem);
                        if (driver.CarIdx == SharedData.Sessions.CurrentSession.FollowedDriver.Driver.CarIdx)
                            driverSelect.SelectedItem = cboxitem;
                    }
                }


                cboxitem = new ComboBoxItem();
                cboxitem.Content = "Most exiting";
                cboxitem.Tag = -1;
                driverSelect.Items.Add(cboxitem);

                cboxitem = new ComboBoxItem();
                cboxitem.Content = "Leader";
                cboxitem.Tag = -2;
                driverSelect.Items.Add(cboxitem);

                cboxitem = new ComboBoxItem();
                cboxitem.Content = "Crashes";
                cboxitem.Tag = -3;
                driverSelect.Items.Add(cboxitem);


                SharedData.updateControls = false;
            }

            if ((simulationAPI != null) && simulationAPI.IsConnected && (simulationAPI.GetData("ReplayPlaySpeed") != null))
            {
                Int32 playspeed = (Int32)simulationAPI.GetData("ReplayPlaySpeed");
                if (playspeed != 1)
                {
                    playButton.Content = "4";
                }
                else
                {
                    playButton.Content = ";";
                }
            }            
            autoCommitEnabled = oldAutoCommit;
        }

        private void autoCommit(object sender, SelectionChangedEventArgs e)
        {
            if (autoCommitEnabled)
                commit();
        }

        private void SwitchCamOrDriver(int driver, int camera)
        {
            if (iRTVOConnection.isServer || !iRTVOConnection.isConnected || !SharedData.remoteClientFollow)
            {
                if (simulationAPI.IsConnected)
                {
                    // Only Execute locally IF 
                    // - i am the server
                    // - i am not connected to a server
                    // - or i am not following the server
                    // Everything else will be handled by the Server
                    simulationAPI.SwitchCamera(driver, camera);
                    Int32 playspeed = getPlaySpeed();
                    Int32 slomo = 0;
                    if (playspeed > 0)
                        slomo = 1;
                    else
                        playspeed = Math.Abs(playspeed);
                    simulationAPI.ReplaySetPlaySpeed(playspeed, slomo);
                }
            }
            // Broadcast IF
            // - I'm the Server
            // - I follow the Server
            if (SharedData.remoteClientFollow || iRTVOConnection.isServer)
            {
                //                    iRTVOConnection.BroadcastMessage("CAMERA", camera);
                //                    iRTVOConnection.BroadcastMessage("DRIVER", driver);
                iRTVOConnection.BroadcastMessage("SWITCH", driver, camera);
                // iRTVOConnection.BroadcastMessage("PLAYSPEED", ((Int32)API.sdk.GetData("ReplayPlaySpeed")), ((bool)API.sdk.GetData("ReplayPlaySlowMotion") ? 1:0));
            }
        }

        private void commit()
        {
            if (driverSelect.SelectedItem != null && cameraSelectComboBox.SelectedItem != null)
            {
                int driver, camera;
                driver = Convert.ToInt32(driverSelect.SelectedValue);
                camera = Convert.ToInt32(cameraSelectComboBox.SelectedValue);
                logger.Trace("Commiting Driver " + driver);
                SwitchCamOrDriver(driver, camera);
                
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
            if (simulationAPI.IsConnected)
            {
                Int32 playspeed = (Int32)simulationAPI.GetData("ReplayPlaySpeed");
                if (playspeed != 1)
                {
                    iRTVOConnection.BroadcastMessage("PLAY");

                    simulationAPI.Play();
                    playButton.Content = "4";
                }
                else
                {
                    iRTVOConnection.BroadcastMessage("PAUSE");

                    simulationAPI.Pause();
                    playButton.Content = ";";
                }
            }
        }

        private void liveButton_Click(object sender, RoutedEventArgs e)
        {
            if (simulationAPI.IsConnected)
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
                bool isSloMo = (bool)simulationAPI.GetData("ReplayPlaySlowMotion");
                BookmarkEvent ev = new BookmarkEvent
                {
                    BookmarkType = BookmarkEventType.Play,
                    ReplayPos = (Int32)simulationAPI.GetData("ReplayFrameNum"),
                    CamIdx = (Int32)simulationAPI.GetData("CamGroupNumber"),
                    DriverIdx = SharedData.Sessions.CurrentSession.FollowedDriver.Driver.NumberPlatePadded,
                    PlaySpeed = isSloMo ? (Int32)simulationAPI.GetData("ReplayPlaySpeed") : (Int32)simulationAPI.GetData("ReplayPlaySpeed") * (-1),
                    Description = "Bookmark " + SharedData.Sessions.CurrentSession.FollowedDriver.Driver.Name,
                    DriverName = SharedData.Sessions.CurrentSession.FollowedDriver.Driver.Name,
                    Timestamp = TimeSpan.FromMilliseconds((Double)simulationAPI.GetData("SessionTime"))
                };
                lock (SharedData.SharedDataLock)
                {
                    SharedData.Bookmarks.SessionID = SharedData.Sessions.SessionId;
                    SharedData.Bookmarks.SubSessionID = SharedData.Sessions.SubSessionId;
                    SharedData.Bookmarks.List.Add(ev);
                }
            }
        }

        private void beginButton_Click(object sender, RoutedEventArgs e)
        {
            BookmarkEvent prevEvent;
            int diff = 0;

            if(SharedData.Bookmarks.List.Count > 0) 
            {
                prevEvent = SharedData.Bookmarks.List[0]; // pick first
                diff = (Int32)simulationAPI.GetData("ReplayFrameNum");

                foreach (BookmarkEvent ev in SharedData.Bookmarks.List)
                {
                    if (((Int32)simulationAPI.GetData("ReplayFrameNum") - ev.ReplayPos) < diff)
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
            try
            {
                BookmarkEvent ev = (BookmarkEvent)input;
                Int32 rewindFrames = (Int32)simulationAPI.GetData("ReplayFrameNum") - (int)ev.ReplayPos;

                simulationAPI.SwitchCamera(ev.DriverIdx, ev.CamIdx);
                simulationAPI.ReplaySetPlayPosition(ReplayPositionModeTypes.Begin, (int)(ev.ReplayPos - (ev.Rewind * 60)));

                Int32 curpos = (Int32)simulationAPI.GetData("ReplayFrameNum");
                DateTime timeout = DateTime.Now;

                // wait rewind to finish, but only 15 secs
                while (curpos != (int)(ev.ReplayPos - (ev.Rewind * 60)) && (DateTime.Now - timeout).TotalSeconds < 15)
                {
                    Thread.Sleep(16);
                    curpos = (Int32)simulationAPI.GetData("ReplayFrameNum");
                }

                SetPlaySpeed(ev.PlaySpeed);

                SharedData.updateControls = true;

                iRTVOConnection.BroadcastMessage("REWIND", rewindFrames, ev.PlaySpeed);
            }
            catch (Exception ex)
            {
                logger.Error("Exception in iRTVO.Conrols:rewind");
                logger.Error(ex.ToString());
            }
        }

        private void SetPlaySpeed(int playspeed)
        {
            int slomo = 0;
            if (playspeed > 0)
                slomo = 1;
            else
            {
                playspeed = Math.Abs(playspeed);
            }
            simulationAPI.ReplaySetPlaySpeed( playspeed, slomo);
        }
        public void live()
        {
            SharedData.triggers.Push(TriggerTypes.live);

            simulationAPI.ReplaySearch( ReplaySearchModeTypes.ToEnd, 0);
            simulationAPI.Play();
            SharedData.updateControls = true;

            iRTVOConnection.BroadcastMessage("LIVE");
        }

        private void instantReplay_Click(object sender, RoutedEventArgs e)
        {
            SharedData.triggers.Push(TriggerTypes.replay);

            ComboBoxItem cbi = (ComboBoxItem)Rewind.SelectedItem;
            string secstr = cbi.Content.ToString();
            int secint = Int32.Parse(secstr.Substring(0, secstr.Length - 1));

            Event ev = new Event(
                Event.eventType.bookmark,
                (Int32)simulationAPI.GetData("ReplayFrameNum") - (secint * 60),
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

            if (btn.Name == "nextDriver")
                nextPos--;
            else if (btn.Name == "prevDriver")
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
            logger.Trace("Sending Driver " + padCarNum(nextPlate));
            if (autoCommitEnabled)
            {
                SwitchCamOrDriver(padCarNum(nextPlate), Convert.ToInt32(cameraSelectComboBox.SelectedValue));
            }
            
        }

        private void PlaySpeed_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SharedData.selectedPlaySpeed = getPlaySpeed();
            autoCommit(sender, e);
        }
    }
}
