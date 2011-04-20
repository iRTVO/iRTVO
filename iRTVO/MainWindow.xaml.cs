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

namespace iRTVO
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        Thread thMacro;

        Macro macro = new Macro();

        // Create overlay
        Window overlayWindow = new Overlay();

        // Create options
        Window options;

        // statusbar update timer
        DispatcherTimer statusBarUpdateTimer = new DispatcherTimer();

        // custom buttons
        StackPanel[] userButtonsRow;
        Button[] buttons;

        // session names
        Dictionary<iRacingTelem.eSessionType, string> sessionNames = new Dictionary<iRacingTelem.eSessionType, string>()
        {
            {iRacingTelem.eSessionType.kSessionTypeGrid, "Gridding"},
            {iRacingTelem.eSessionType.kSessionTypeInvalid, "Invalid"},
            {iRacingTelem.eSessionType.kSessionTypePractice, "Practice"},
            {iRacingTelem.eSessionType.kSessionTypePracticeLone, "Practice"},
            {iRacingTelem.eSessionType.kSessionTypeQualifyLone, "Qualify"},
            {iRacingTelem.eSessionType.kSessionTypeQualifyOpen, "Qualify"},
            {iRacingTelem.eSessionType.kSessionTypeRace, "Race"},
            {iRacingTelem.eSessionType.kSessionTypeTesting, "Testing"}
        };

        public MainWindow()
        {
            InitializeComponent();
            // set window position
            this.Left = Properties.Settings.Default.MainWindowLocationX;
            this.Top = Properties.Settings.Default.MainWindowLocationY;
            this.Width = Properties.Settings.Default.MainWindowWidth;
            this.Height = Properties.Settings.Default.MainWindowHeight;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            overlayWindow.Show();

            // start statusbar
            statusBarUpdateTimer.Tick += new EventHandler(updateStatusBar);
            statusBarUpdateTimer.Tick += new EventHandler(updateButtons);
            statusBarUpdateTimer.Tick += new EventHandler(checkWebUpdate);
            statusBarUpdateTimer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            statusBarUpdateTimer.Start();
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

            for(int i = 0; i < SharedData.sessions.Length; i++) {
                if(SharedData.sessions[i].type != iRacingTelem.eSessionType.kSessionTypeInvalid)
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

                for(int i = 0; i < SharedData.sessions.Length; i++) {
                    if(SharedData.sessions[i].type != iRacingTelem.eSessionType.kSessionTypeInvalid) {
                        cboxitem = new ComboBoxItem();
                        cboxitem.Content = i.ToString() + ": " + sessionNames[SharedData.sessions[i].type];
                        comboBoxSession.Items.Add(cboxitem);
                    }
                }

                if(selected != null)
                    comboBoxSession.Text = selected;
                else
                    comboBoxSession.Text = "current";
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
                for (int i = 0; i < SharedData.theme.buttons[buttonId].actions.Length; i++)
                {
                    Theme.ButtonActions action = (Theme.ButtonActions)i;
                    if (SharedData.theme.buttons[buttonId].actions[i] != null)
                    {
                        if (ClickAction(action, SharedData.theme.buttons[buttonId].actions[i]))
                            ClickAction(Theme.ButtonActions.hide, SharedData.theme.buttons[buttonId].actions[i]);
                    }
                }
            }
        }

        private Boolean ClickAction(Theme.ButtonActions action, string[] objects)
        {
            for (int j = 0; j < objects.Length; j++)
            {
                if (action == Theme.ButtonActions.replay) // replay control
                {
                    if (objects[0] == "live")
                    {
                        thMacro = new Thread(macro.live);
                        thMacro.Start();
                    }
                    else
                    {
                        thMacro = new Thread(macro.rewind);
                        thMacro.Start(Int32.Parse(objects[0]));
                    }
                }
                else
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

                                    if (SharedData.lastPage[k] == true && SharedData.theme.objects[k].dataset == Theme.dataset.standing)
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
                        default:
                            break;
                    }
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

        private void updateStatusBar(object sender, EventArgs e)
        {
            switch(SharedData.apiState) 
            {
                case SharedData.ConnectionState.active:
                    statusBarState.Text = "Running";
                    break;
                case SharedData.ConnectionState.connecting:
                    statusBarState.Text = "Connecting";
                    break;
                case SharedData.ConnectionState.initializing:
                    statusBarState.Text = "Initializing";
                    break;
                default:
                    statusBarState.Text = "No API connection";
                    break;
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

            //web.postStanding();
            
        }

        private void checkWebUpdate(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.webTimingEnable && 
                (SharedData.sessions[SharedData.currentSession].state != iRacingTelem.eSessionState.kSessionStateInvalid) && 
                SharedData.runOverlay)
            {
                for (int i = 0; i < SharedData.webUpdateWait.Length; i++)
                {
                    if (SharedData.webUpdateWait[i] == true)
                    {
                        switch ((webTiming.postTypes)i)
                        {
                            case webTiming.postTypes.drivers:
                                ThreadPool.QueueUserWorkItem(SharedData.web.postDrivers);
                                break;
                            case webTiming.postTypes.sessions:
                                ThreadPool.QueueUserWorkItem(SharedData.web.postSessions);
                                break;
                            case webTiming.postTypes.standing:
                                ThreadPool.QueueUserWorkItem(SharedData.web.postStanding);
                                break;
                            case webTiming.postTypes.track:
                                ThreadPool.QueueUserWorkItem(SharedData.web.postTrack);
                                break;
                            case webTiming.postTypes.cars:
                                ThreadPool.QueueUserWorkItem(SharedData.web.postCars);
                                break;
                        }
                        SharedData.webUpdateWait[i] = false;
                    }
                }
            }
        }


        private void CloseProgram()
        {
            SharedData.runApi = false;
            overlayWindow.Close();
            Application.Current.Shutdown(0);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CloseProgram();
        }

        private void setReplay()
        {
            SharedData.replayInProgress = true;
        }

        private void hideButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < SharedData.theme.objects.Length; i++)
                SharedData.theme.objects[i].visible = false;

            for (int i = 0; i < SharedData.theme.images.Length; i++)
                SharedData.theme.images[i].visible = false;

            for (int i = 0; i < SharedData.theme.tickers.Length; i++)
                SharedData.theme.tickers[i].visible = false;

            for (int i = 0; i < SharedData.theme.videos.Length; i++)
                SharedData.theme.videos[i].visible = false;
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
                    SharedData.overlaySession = SharedData.currentSession;
                else
                {
                    string[] split = cbi.Content.ToString().Split(':');
                    SharedData.overlaySession = Int32.Parse(split[0]);
                }
            }
        }
    }
}
