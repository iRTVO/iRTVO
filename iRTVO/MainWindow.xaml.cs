/* 
 * MainWindow.xaml.cs
 * 
 * Functionality of the MainWindow (program controls)
 * 
 */
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
using System.Windows.Navigation;
using System.Windows.Shapes;

// additional
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Interop;
using System.IO;

namespace iRTVO
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // Create overlay
        Window overlayWindow = new Overlay();

        // Create options
        Window options;

        // Create controls
        Window controlsWindow = new Controls();

        // Create lists
        Window listsWindow;

        // update timer
        DispatcherTimer updateTimer = new DispatcherTimer();
        
        // trigger timer
        DispatcherTimer triggerTimer = new DispatcherTimer();

        // custom buttons
        StackPanel[] userButtonsRow;
        Button[] buttons;

        // web update wait
        Int16 webUpdateWait = 0;

        // API
        iRacingAPI API;

        public MainWindow()
        {
            InitializeComponent();
            // set window position
            this.Left = Properties.Settings.Default.MainWindowLocationX;
            this.Top = Properties.Settings.Default.MainWindowLocationY;
            this.Width = Properties.Settings.Default.MainWindowWidth;
            this.Height = Properties.Settings.Default.MainWindowHeight;

            if (Properties.Settings.Default.AoTmain == true)
                this.Topmost = true;
            else
                this.Topmost = false;

            SharedData.serverThread = new Thread(startServer);

            API = new iRTVO.iRacingAPI();
            API.sdk.Startup();

            if (API.sdk.IsConnected())
                cameraNum = (Int32)API.sdk.GetData("CamCameraNumber");

            // autostart client/server
            if (Properties.Settings.Default.remoteClientAutostart)
            {
                Button dummyButton = new Button();
                this.bClient_Click(dummyButton, new RoutedEventArgs());
            }

            if (Properties.Settings.Default.remoteServerAutostart)
            {
                Button dummyButton = new Button();
                this.bServer_Click(dummyButton, new RoutedEventArgs());
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            overlayWindow.Show();
            //overlayWindow.Hide();
            controlsWindow.Show();

            // start timers
            updateTimer.Tick += new EventHandler(updateStatusBar);
            updateTimer.Tick += new EventHandler(updateButtons);
            updateTimer.Tick += new EventHandler(checkWebUpdate);
            updateTimer.Tick += new EventHandler(pageSwitcher);
            updateTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            updateTimer.Start();
            
            // trigger timer runs same speed as the overlay
            int updateMs = (int)Math.Round(1000 / (double)Properties.Settings.Default.UpdateFrequency);
            triggerTimer.Tick += new EventHandler(triggerTimer_Tick);
            triggerTimer.Tick += new EventHandler(serverTimer_Tick);
            triggerTimer.Interval = new TimeSpan(0, 0, 0, 0, updateMs);
            triggerTimer.Start();
        }

        // trigger handler
        void triggerTimer_Tick(object sender, EventArgs e)
        {
            TriggerTypes trigger;
            while (SharedData.triggers.Count > 0)
            {
                trigger = (TriggerTypes)SharedData.triggers.Pop();
                int triggerId = -1;
                
                // search matching trigger and pick first
                for (int i = 0; i < SharedData.theme.triggers.Length; i++)
                {
                    if (SharedData.theme.triggers[i].name.ToLower() == trigger.ToString().ToLower())
                    {
                        triggerId = i;
                        break;
                    }
                }

                // if trigger found execute it
                if (triggerId >= 0)
                {
                    for (int i = 0; i < SharedData.theme.triggers[triggerId].actions.Length; i++)
                    {
                        Theme.ButtonActions action = (Theme.ButtonActions)i;
                        if (SharedData.theme.triggers[triggerId].actions[i] != null)
                        {
                            ClickAction(action, SharedData.theme.triggers[triggerId].actions[i]);
                        }
                    }
                }
            }
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

        private void updateButtons(object sender, EventArgs e)
        {
            if (SharedData.refreshButtons == true)
            {
                //userButtonsRows.Children.Clear();
                buttonStackPanel.Children.RemoveRange(1, buttonStackPanel.Children.Count-1);

                int rowCount = 0;
                for (int i = 0; i < SharedData.theme.buttons.Length; i++)
                {
                    if (SharedData.theme.buttons[i].row > rowCount)
                        rowCount = SharedData.theme.buttons[i].row;
                }

                userButtonsRow = new StackPanel[rowCount + 1];

                for (int i = 0; i < userButtonsRow.Length; i++)
                {
                    userButtonsRow[i] = new StackPanel();
                    buttonStackPanel.Children.Add(userButtonsRow[i]);
                }

                //RoutedEventArgs args;

                buttons = new Button[SharedData.theme.buttons.Length];
                for (int i = 0; i < SharedData.theme.buttons.Length; i++)
                {
                    //args = i;
                    buttons[i] = new Button();
                    buttons[i].Content = SharedData.theme.buttons[i].text;
                    buttons[i].Click += new RoutedEventHandler(HandleClick);
                    buttons[i].Name = "customButton" + i.ToString();
                    buttons[i].Margin = new Thickness(3);
                    userButtonsRow[SharedData.theme.buttons[i].row].Children.Add(buttons[i]);
                }

                SharedData.refreshButtons = false;
            }
            
            int sessions = 0;

            for (int i = 0; i < SharedData.Sessions.SessionList.Count; i++)
            {
                if (SharedData.Sessions.SessionList[i].Type != Sessions.SessionInfo.sessionType.invalid)
                    sessions++;
            }

            ComboBoxItem cboxitem;
            string selected = null;

            if (comboBoxSession.HasItems)
            {
                cboxitem = (ComboBoxItem)comboBoxSession.SelectedItem;
                selected = cboxitem.Content.ToString();
            }

            if (comboBoxSession.Items.Count != (sessions + 1))
            {
                comboBoxSession.Items.Clear();
                cboxitem = new ComboBoxItem();
                cboxitem.Content = "current";
                comboBoxSession.Items.Add(cboxitem);

                for (int i = 0; i < SharedData.Sessions.SessionList.Count; i++)
                {
                    if (SharedData.Sessions.SessionList[i].Type != Sessions.SessionInfo.sessionType.invalid)
                    {
                        cboxitem = new ComboBoxItem();
                        cboxitem.Content = i.ToString() + ": " + SharedData.Sessions.SessionList[i].Type.ToString();
                        comboBoxSession.Items.Add(cboxitem);
                    }
                }

                if(selected != null)
                    comboBoxSession.Text = selected;
                else
                    comboBoxSession.Text = "current";
            }
        }

        private void pageSwitcher(object sender, EventArgs e)
        {
            for (int i = 0; i < SharedData.theme.buttons.Length; i++)
            {
                if (SharedData.theme.buttons[i].active == true &&
                   (DateTime.Now - SharedData.theme.buttons[i].pressed).TotalSeconds >= SharedData.theme.buttons[i].delay)
                {
                    Button dummyButton = new Button();
                    dummyButton.Name = "customButton" + i.ToString();
                    dummyButton.Content = "";
                    this.HandleClick(dummyButton, new RoutedEventArgs());
                }
            }
        }

        void HandleClick(object sender, RoutedEventArgs e)
        {
            Button button = new Button();

            try
            {
                button = (Button)sender;
            }
            finally
            {
                int buttonId = Int32.Parse(button.Name.Substring(12));

                if(button.Content != null && button.Content.ToString() != "") {
                    if (SharedData.remoteClient != null) // empty button means page switchers dummy button which is ignored
                    {
                        SharedData.remoteClient.sendMessage("BUTTON;" + button.Name);
                    }
                    else if (SharedData.serverThread.IsAlive)
                    {
                        SharedData.serverOutBuffer.Push("BUTTON;" + button.Name);
                    }
                }

                if (SharedData.theme.buttons[buttonId].delay > 0)
                {
                    SharedData.theme.buttons[buttonId].pressed = DateTime.Now;
                    SharedData.theme.buttons[buttonId].active = true;
                }

                for (int i = 0; i < SharedData.theme.buttons[buttonId].actions.Length; i++)
                {
                    Theme.ButtonActions action = (Theme.ButtonActions)i;
                    if (SharedData.theme.buttons[buttonId].actions[i] != null)
                    {
                        if (ClickAction(action, SharedData.theme.buttons[buttonId].actions[i]))
                        {
                            if (SharedData.theme.buttons[buttonId].delayLoop)
                            {
                                ClickAction(action, SharedData.theme.buttons[buttonId].actions[i]);
                                Console.WriteLine("Last page and skipping to first");
                            }
                            else {
                                ClickAction(Theme.ButtonActions.hide, SharedData.theme.buttons[buttonId].actions[i]);
                                SharedData.theme.buttons[buttonId].active = false;
                                Console.WriteLine("Last page and hiding");
                            }
                        }
                    }
                }
            }
        }

        private Boolean ClickAction(Theme.ButtonActions action, string[] objects)
        {
            for (int j = 0; j < objects.Length; j++)
            {
                string[] split = objects[j].Split('-');
                switch (split[0])
                {
                    case "Overlay": // overlays
                        for (int k = 0; k < SharedData.theme.objects.Length; k++)
                        {
                            if (SharedData.theme.objects[k].name == split[1])
                            {

                                if (SharedData.theme.objects[k].dataset == Theme.dataset.standing && action == Theme.ButtonActions.show)
                                {
                                    SharedData.theme.objects[k].page++;
                                }

                                if (SharedData.lastPage[k] == true && SharedData.theme.objects[k].dataset == Theme.dataset.standing && action == Theme.ButtonActions.show)
                                {
                                    SharedData.theme.objects[k].visible = setObjectVisibility(SharedData.theme.objects[k].visible, Theme.ButtonActions.hide);
                                    SharedData.theme.objects[k].page = -1;
                                    SharedData.lastPage[k] = false;

                                    return true;
                                }
                                else
                                {
                                    SharedData.theme.objects[k].visible = setObjectVisibility(SharedData.theme.objects[k].visible, action);
                                }
                            }
                        }
                        break;
                    case "Image": // images
                        for (int k = 0; k < SharedData.theme.images.Length; k++)
                        {
                            if (SharedData.theme.images[k].name == split[1])
                            {
                                SharedData.theme.images[k].visible = setObjectVisibility(SharedData.theme.images[k].visible, action);
                            }
                        }
                        break;
                    case "Ticker": // tickers
                        for (int k = 0; k < SharedData.theme.tickers.Length; k++)
                        {
                            if (SharedData.theme.tickers[k].name == split[1])
                            {
                                SharedData.theme.tickers[k].visible = setObjectVisibility(SharedData.theme.tickers[k].visible, action);
                            }
                        }
                        break;
                    case "Video": // video
                        for (int k = 0; k < SharedData.theme.videos.Length; k++)
                        {
                            if (SharedData.theme.videos[k].name == split[1])
                            {
                                SharedData.theme.videos[k].visible = setObjectVisibility(SharedData.theme.videos[k].visible, action);
                            }
                        }
                        break;
                    case "Sound": // sound
                        for (int k = 0; k < SharedData.theme.sounds.Length; k++)
                        {
                            if (SharedData.theme.sounds[k].name == split[1])
                            {
                                switch (action)
                                {
                                    case Theme.ButtonActions.hide:
                                        SharedData.theme.sounds[k].playing = false;
                                        break;
                                    default:
                                        SharedData.theme.sounds[k].playing = true;
                                        break;
                                }
                            }
                        }
                        break;
                    case "Trigger":
                        switch (split[1])
                        {
                            case "flags":
                                if (SharedData.Sessions.CurrentSession.Flag == Sessions.SessionInfo.sessionFlag.white)
                                    SharedData.triggers.Push(TriggerTypes.flagWhite);
                                else if (SharedData.Sessions.CurrentSession.Flag == Sessions.SessionInfo.sessionFlag.checkered)
                                    SharedData.triggers.Push(TriggerTypes.flagCheckered);
                                else if (SharedData.Sessions.CurrentSession.Flag == Sessions.SessionInfo.sessionFlag.yellow)
                                    SharedData.triggers.Push(TriggerTypes.flagYellow);
                                else
                                    SharedData.triggers.Push(TriggerTypes.flagGreen);
                                break;
                            case "lights":
                                if (SharedData.Sessions.CurrentSession.StartLight == Sessions.SessionInfo.sessionStartLight.ready)
                                    SharedData.triggers.Push(TriggerTypes.lightsReady);
                                else if(SharedData.Sessions.CurrentSession.StartLight == Sessions.SessionInfo.sessionStartLight.set)
                                    SharedData.triggers.Push(TriggerTypes.lightsSet);
                                else if (SharedData.Sessions.CurrentSession.StartLight == Sessions.SessionInfo.sessionStartLight.go)
                                    SharedData.triggers.Push(TriggerTypes.lightsGo);
                                else
                                    SharedData.triggers.Push(TriggerTypes.lightsOff);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
            return false;
        }

        private Boolean setObjectVisibility(Boolean currentValue, Theme.ButtonActions action)
        {
            if (action == Theme.ButtonActions.hide)
                return false;
            else if (action == Theme.ButtonActions.show)
                return true;
            else if (action == Theme.ButtonActions.toggle)
            {
                if (currentValue == true)
                    return false;
                else
                    return true;
            }
            else
                return true;
        }

        private string formatBytes(Int64 bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = 0;
            for (i = 0; (int)(bytes / 1024) > 0; i++, bytes /= 1024)
                dblSByte = bytes / 1024.0;
            return String.Format("{0:0.00} {1}", dblSByte, Suffix[i]);
        }

        private void updateStatusBar(object sender, EventArgs e)
        {
            if(SharedData.apiConnected) 
            {
                if(!overlayWindow.IsVisible)
                    overlayWindow.Show();
                statusBarState.Text = "Sim: Running";
            }
            else 
            {
                statusBarState.Text = "Sim: No connection";
            }

            int count = SharedData.overlayFPSstack.Count() * 1000;
            float totaltime = 0;
            foreach (float frametime in SharedData.overlayFPSstack)
                totaltime += frametime;
            double fps = Math.Round(count / totaltime);
            SharedData.overlayFPSstack.Clear();
            statusBarFps.Text = fps.ToString() + " fps";

            count = SharedData.overlayEffectiveFPSstack.Count() * 1000;
            totaltime = 0;
            foreach (float frametime in SharedData.overlayEffectiveFPSstack)
                totaltime += frametime;
            double eff_fps = Math.Round(count / totaltime);
            SharedData.overlayEffectiveFPSstack.Clear();

            statusBarFps.ToolTip = string.Format("fps: {0}, effective fps: {1}",  fps, eff_fps);

            if (Properties.Settings.Default.webTimingEnable &&
                (SharedData.Sessions.CurrentSession.State != Sessions.SessionInfo.sessionState.invalid) &&
                SharedData.runOverlay)
            {
                statusBarWebTiming.Text = "Web: enabled";

                Brush textColor = System.Windows.SystemColors.WindowTextBrush;
                if(SharedData.webError.Length > 0)
                    textColor = System.Windows.Media.Brushes.Red;

                statusBarWebTiming.Foreground = textColor;
            }
            else
            {
                statusBarWebTiming.Text = "Web: disabled";
            }

            if (SharedData.webError.Length > 0)
                statusBarWebTiming.ToolTip = string.Format("Error: {0}", SharedData.webError); 
            else
                statusBarWebTiming.ToolTip = string.Format("Out: {0}", formatBytes(SharedData.webBytes));

            if (comboBoxSession.SelectedItem != null)
            {
                ComboBoxItem cbi = (ComboBoxItem)comboBoxSession.SelectedItem;
                if (cbi.Content.ToString() == "current")
                {
                    SharedData.overlaySession = SharedData.Sessions.CurrentSession.Id;
                }
            }

            if (SharedData.cacheMiss > 0 && SharedData.cacheHit > 0 && SharedData.cacheFrameCount > 0)
            {
                //Console.WriteLine(((float)SharedData.cacheHit / ((float)SharedData.cacheMiss + (float)SharedData.cacheHit)) * 100 + " " + (SharedData.cacheMiss + SharedData.cacheHit)/SharedData.cacheFrameCount + " per frame");
                statusBarFps.ToolTip = statusBarFps.ToolTip + string.Format("\ncache hitrate: {0:00}% for {1} objects", ((float)SharedData.cacheHit / ((float)SharedData.cacheMiss + (float)SharedData.cacheHit)) * 100, (SharedData.cacheMiss + SharedData.cacheHit) / SharedData.cacheFrameCount);
                SharedData.cacheMiss = 0;
                SharedData.cacheHit = 0;
                SharedData.cacheFrameCount = 0;
            }
            
        }

        private void checkWebUpdate(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.webTimingEnable &&
                (SharedData.Sessions.CurrentSession.State != Sessions.SessionInfo.sessionState.invalid) &&
                SharedData.runOverlay &&
                webUpdateWait > Properties.Settings.Default.webTimingInterval)
            {
                ThreadPool.QueueUserWorkItem(SharedData.web.postData);
                webUpdateWait = 0;
            }
            else
            {
                webUpdateWait++;
            }
        }

        private void CloseProgram()
        {
            SharedData.serverThreadRun = false;
            SharedData.runApi = false;

            if (overlayWindow != null)
                overlayWindow.Close();
            if (controlsWindow != null)
                controlsWindow.Close();
            if (listsWindow != null)
                listsWindow.Close();

            /*
            if (SharedData.serverThread.IsAlive)
            {
                SharedData.serverThread.Abort();
                SharedData.serverThread.Join();
            }
             * */

            string[] args = Environment.CommandLine.Split(' ');
            if (args.Length > 2 && args[args.Length - 1] == "--debug")
                SharedData.writeCache(SharedData.Sessions.SessionId);

            Application.Current.Shutdown(0);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseProgram();
        }

        private void hideButton_Click(object sender, RoutedEventArgs e)
        {
            // skipping sending if the sender is dummy button, i.e. this was received command ands shouldn't be sent back
            Button button = new Button();
            try
            {
                button = (Button)sender;
            }
            finally
            {
                if (button.Content != null && button.Content.ToString() != "")
                {
                    if (SharedData.remoteClient != null)
                        SharedData.remoteClient.sendMessage("HIDE;");
                    else if (SharedData.serverThread.IsAlive)
                        SharedData.serverOutBuffer.Push("HIDE;");
                }
            }

            for (int i = 0; i < SharedData.theme.buttons.Length; i++)
                SharedData.theme.buttons[i].active = false;

            for (int i = 0; i < SharedData.theme.objects.Length; i++)
                SharedData.theme.objects[i].visible = false;

            for (int i = 0; i < SharedData.theme.images.Length; i++)
                SharedData.theme.images[i].visible = false;

            for (int i = 0; i < SharedData.theme.tickers.Length; i++)
                SharedData.theme.tickers[i].visible = false;

            for (int i = 0; i < SharedData.theme.videos.Length; i++)
                SharedData.theme.videos[i].visible = false;

            for (int i = 0; i < SharedData.theme.sounds.Length; i++)
                SharedData.theme.sounds[i].playing = false;
        }

        private void Main_LocationChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MainWindowLocationX = (int)this.Left;
            Properties.Settings.Default.MainWindowLocationY = (int)this.Top;
            Properties.Settings.Default.Save();
        }

        private void bOptions_Click(object sender, RoutedEventArgs e)
        {
            if (options == null || options.IsVisible == false)
            {
                options = new Options();
                options.Show();
            }
            options.Activate();
        }

        private void Main_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Properties.Settings.Default.MainWindowWidth = (int)this.Width;
            Properties.Settings.Default.MainWindowHeight = (int)this.Height;
            Properties.Settings.Default.Save();
        }

        private void comboBoxSession_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxSession.Items.Count > 0)
            {
                ComboBoxItem cbi = (ComboBoxItem)comboBoxSession.SelectedItem;
                if (cbi.Content.ToString() == "current")
                    SharedData.overlaySession = SharedData.Sessions.CurrentSession.Id;
                else
                {
                    string[] split = cbi.Content.ToString().Split(':');
                    SharedData.overlaySession = Int32.Parse(split[0]);
                }
            }
        }

        private void bAbout_Click(object sender, RoutedEventArgs e)
        {
            Window about = new about();
            about.Show();
        }

        private void controlsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!controlsWindow.IsVisible)
            {
                controlsWindow = new Controls();
                controlsWindow.Show();
            }
            controlsWindow.Activate();
        }

        private void listsButton_Click(object sender, RoutedEventArgs e)
        {
            if (listsWindow == null || !listsWindow.IsVisible)
            {
                listsWindow = new Lists();
                listsWindow.Show();
            }
            listsWindow.Activate();
        }

        private static void startServer()
        {
            remoteServer server = new remoteServer(Properties.Settings.Default.remoteServerPort);
        }

        private void bServer_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.serverThread.IsAlive == false)
            {
                SharedData.serverThreadRun = true;
                SharedData.serverThread = new Thread(startServer);
                SharedData.serverThread.Start();
                this.bClient.IsEnabled = false;
                SharedData.executeBuffer = new Stack<string>();

            }
            else
            {
                SharedData.serverThreadRun = false;
                this.bClient.IsEnabled = true;
            }
        }

        private void bClient_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.remoteClient == null)
            {
                SharedData.remoteClient = new remoteClient(Properties.Settings.Default.remoteClientIp, Properties.Settings.Default.remoteClientPort);
                this.bServer.IsEnabled = false;
                SharedData.executeBuffer = new Stack<string>();
            }
            else
            {
                SharedData.remoteClient = null;
                this.bServer.IsEnabled = true;
            }
        }

        // clients execute commands from server
        void serverTimer_Tick(object sender, EventArgs e) 
        {
            Int32 cameraNum = -1;
            Int32 driverNum = -1;

            if (SharedData.executeBuffer.Count > 0 && SharedData.remoteClientFollow)
            {
                while (SharedData.executeBuffer.Count > 0)
                {
                    string[] cmd = SharedData.executeBuffer.Pop().Split(';');
                    Button dummyButton;
                    switch (cmd[0])
                    {
                        case "BUTTON":
                            dummyButton = new Button();
                            dummyButton.Name = cmd[1];
                            this.HandleClick(dummyButton, new RoutedEventArgs());
                            break;
                        case "RESET":
                            dummyButton = new Button();
                            dummyButton.Name = null;
                            this.bReset_Click(dummyButton, new RoutedEventArgs());
                            break;
                        case "HIDE":
                            dummyButton = new Button();
                            dummyButton.Name = null;
                            this.hideButton_Click(dummyButton, new RoutedEventArgs());
                            break;
                        case "CAMERA":
                            cameraNum = Int32.Parse(cmd[1]);
                            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, -1, cameraNum);
                            break;
                        case "DRIVER":
                            driverNum = Int32.Parse(cmd[1]);
                            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, driverNum, 0);
                            break;
                        case "REWIND":
                            if (!SharedData.remoteClientSkipRewind)
                            {
                                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlayPosition, (int)iRSDKSharp.ReplayPositionModeTypes.Begin, ((Int32)API.sdk.GetData("ReplayFrameNum") - Int32.Parse(cmd[1])));
                                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
                                SharedData.updateControls = true;
                                SharedData.triggers.Push(TriggerTypes.replay);
                            }
                            break;
                        case "LIVE":
                            if (!SharedData.remoteClientSkipRewind)
                            {
                                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySearch, (int)iRSDKSharp.ReplaySearchModeTypes.ToEnd, 0);
                                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
                                SharedData.updateControls = true;
                                SharedData.triggers.Push(TriggerTypes.live);
                            }
                            break;
                        case "PLAY":
                            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
                            SharedData.updateControls = true;
                            break;
                        case "PAUSE":
                            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 0, 0);
                            SharedData.updateControls = true;
                            break;
                        default:
                            Console.WriteLine("Caught odd command: " + cmd[0]);
                            break;
                    }
                }
                SharedData.remoteClientSkipRewind = false;
            }

            // update buttons
            if (SharedData.remoteClient != null)
                bClient.Content = "[connected]";
            else
                bClient.Content = "Connect client";

            if (SharedData.serverThreadRun)
                bServer.Content = "[running]";
            else
                bServer.Content = "Start server";

            if (SharedData.remoteClientFollow)
                bClientFollow.Content = "Stop following";
            else
                bClientFollow.Content = "Follow server";

            if (SharedData.remoteClient != null)
                bClientFollow.IsEnabled = true;
            else
                bClientFollow.IsEnabled = false;
        }

        private void bReset_Click(object sender, RoutedEventArgs e)
        {
            string[] args = Environment.CommandLine.Split(' ');
            if (args.Length > 2 && args[args.Length - 1] == "--debug")
                SharedData.writeCache(SharedData.Sessions.SessionId);

            updateTimer.Stop();
            triggerTimer.Stop();

            SharedData.runApi = false;

            if (overlayWindow != null)
                overlayWindow.Close();
            if (controlsWindow != null)
                controlsWindow.Close();
            if (listsWindow != null)
                listsWindow.Close();

            SharedData.runApi = true;
            SharedData.runOverlay = false;
            SharedData.apiConnected = false;
            SharedData.isLive = true;

            // Data
            SharedData.Drivers = new List<DriverInfo>();
            SharedData.Sessions = new Sessions();
            SharedData.Track = new TrackInfo();
            SharedData.Camera = new CameraInfo();
            SharedData.Events = new Events();
            SharedData.Bookmarks = new Bookmarks();
            SharedData.Sectors = new List<Single>();
            SharedData.SelectedSectors = new List<Single>();
            SharedData.Classes = new Int32[3] {-1, -1, -1};

            // Update stuff
            SharedData.updateControls = false;
            SharedData.showSimUi = true;

            overlayWindow = new Overlay();
            controlsWindow = new Controls();

            overlayWindow.Show();
            controlsWindow.Show();

            updateTimer.Start();
            triggerTimer.Start();

            SharedData.writeMutex = new Mutex();
            SharedData.readMutex = new Mutex();

            // skipping sending if the sender is dummy button, i.e. this was received command ands shouldn't be sent back
            Button button = new Button();
            try
            {
                button = (Button)sender;
            }
            finally
            {
                if (button.Content != null && button.Content.ToString() != "")
                {
                    if (SharedData.remoteClient != null)
                        SharedData.remoteClient.sendMessage("RESET;");
                    else if (SharedData.serverThread.IsAlive)
                        SharedData.serverOutBuffer.Push("RESET;");
                }
            }


        }

        private void comboBoxClass_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBoxClass.Items.Count > 0)
            {
                ComboBoxItem cbi = (ComboBoxItem)comboBoxClass.SelectedItem;
                if (cbi.Content.ToString() == "auto")
                    SharedData.overlayClass = null;
                else
                {
                    SharedData.overlayClass = cbi.Content.ToString();
                }
            }
        }

        private void bClientFollow_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.remoteClientFollow)
            {
                SharedData.remoteClientFollow = false;
                SharedData.remoteClientSkipRewind = true;
            }
            else
                SharedData.remoteClientFollow = true;
        }
    }
}
