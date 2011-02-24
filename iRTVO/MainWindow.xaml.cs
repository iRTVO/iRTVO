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
                userButtons.Children.Clear();
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
                    userButtons.Children.Add(buttons[i]);
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
                    case "Ticker":
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

        private void bQuit_Click(object sender, RoutedEventArgs e)
        {
            CloseProgram();
        }

        private void replayButton_Click(object sender, RoutedEventArgs e)
        {
            //SharedData.visible[(int)SharedData.overlayObjects.sessionstate] = false;
            //overlay.Fill = new SolidColorBrush(Colors.Black);
            //Refresh(overlay);

            thMacro = new Thread(macro.rewind);
            thMacro.Start(Int32.Parse(rewindTextbox.Text));
            thMacro.Join();

            //overlay.Fill = null;
            //SharedData.visible[(int)SharedData.overlayObjects.replay] = true;
        }

        private void liveButton_Click(object sender, RoutedEventArgs e)
        {
            //SharedData.visible[(int)SharedData.overlayObjects.replay] = false;
            macro.live();
        }

        private void hideButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < SharedData.theme.objects.Length; i++)
                SharedData.theme.objects[i].visible = false;

            for (int i = 0; i < SharedData.theme.images.Length; i++)
                SharedData.theme.images[i].visible = false;
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

        /*
        private void bName_Click(object sender, RoutedEventArgs e)
        {
            SharedData.theme.objects[0].visible = true;
            SharedData.theme.images[0].visible = true;
        }

        private void sidepanelDiffSelectLeader_Checked(object sender, RoutedEventArgs e)
        {
            SharedData.sidepanelType = SharedData.sidepanelTypes.leader;
        }

        private void sidepanelDiffSelectFollowed_Checked(object sender, RoutedEventArgs e)
        {
            SharedData.sidepanelType = SharedData.sidepanelTypes.followed;
        }

        private void sidepanelDiffSelectFastlap_Checked(object sender, RoutedEventArgs e)
        {
            SharedData.sidepanelType = SharedData.sidepanelTypes.fastlap;
        }

        private void practiceResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.results] == false)
            {
                SharedData.resultPage = 0;
                qualifyResultsButton.IsEnabled = false;
                raceResultsButton.IsEnabled = false;
                for (int i = 0; i < SharedData.visible.Length; i++)
                    SharedData.visible[i] = false;
            }
            else if (SharedData.visible[(int)SharedData.overlayObjects.results] == true && SharedData.resultLastPage == true)
            {
                SharedData.visible[(int)SharedData.overlayObjects.results] = false;
                SharedData.resultSession = -1;
                SharedData.resultPage = -1;
                SharedData.resultLastPage = false;
                qualifyResultsButton.IsEnabled = true;
                raceResultsButton.IsEnabled = true;
            }
            else
                SharedData.resultPage++;

            if (SharedData.resultPage >= 0)
            {
                for (int i = 0; i < iRacingTelem.MAX_SESSIONS; i++)
                {
                    if (SharedData.sessions[i].type == iRacingTelem.eSessionType.kSessionTypePractice)
                        SharedData.resultSession = i;
                }

                if (SharedData.resultSession >= 0)
                {
                    SharedData.visible[(int)SharedData.overlayObjects.results] = true;
                }
                else
                {
                    qualifyResultsButton.IsEnabled = true;
                    raceResultsButton.IsEnabled = true;
                    SharedData.resultPage = -1;
                }
            }
        }

        private void qualifyResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.results] == false)
            {
                SharedData.resultPage = 0;
                practiceResultsButton.IsEnabled = false;
                raceResultsButton.IsEnabled = false;
                for (int i = 0; i < SharedData.visible.Length; i++)
                    SharedData.visible[i] = false;
            }
            else if (SharedData.visible[(int)SharedData.overlayObjects.results] == true && SharedData.resultLastPage == true)
            {
                SharedData.visible[(int)SharedData.overlayObjects.results] = false;
                SharedData.resultSession = -1;
                SharedData.resultPage = -1;
                SharedData.resultLastPage = false;
                practiceResultsButton.IsEnabled = true;
                raceResultsButton.IsEnabled = true;
            }
            else
                SharedData.resultPage++;

            if (SharedData.resultPage >= 0)
            {
                for (int i = 0; i < iRacingTelem.MAX_SESSIONS; i++)
                {
                    if (SharedData.sessions[i].type == iRacingTelem.eSessionType.kSessionTypeQualifyLone ||
                        SharedData.sessions[i].type == iRacingTelem.eSessionType.kSessionTypeQualifyOpen)
                        SharedData.resultSession = i;
                }

                if (SharedData.resultSession >= 0)
                {
                    SharedData.visible[(int)SharedData.overlayObjects.results] = true;
                }
                else
                {
                    practiceResultsButton.IsEnabled = true;
                    raceResultsButton.IsEnabled = true;
                    SharedData.resultPage = -1;
                }
            }
        }

        private void raceResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.results] == false)
            {
                SharedData.resultPage = 0;
                practiceResultsButton.IsEnabled = false;
                qualifyResultsButton.IsEnabled = false;
                for (int i = 0; i < SharedData.visible.Length; i++)
                    SharedData.visible[i] = false;
            }
            else if (SharedData.visible[(int)SharedData.overlayObjects.results] == true && SharedData.resultLastPage == true)
            {
                SharedData.visible[(int)SharedData.overlayObjects.results] = false;
                SharedData.resultSession = -1;
                SharedData.resultPage = -1;
                SharedData.resultLastPage = false;
                practiceResultsButton.IsEnabled = true;
                qualifyResultsButton.IsEnabled = true;
            }
            else
                SharedData.resultPage++;

            if (SharedData.resultPage >= 0)
            {
                for (int i = 0; i < iRacingTelem.MAX_SESSIONS; i++)
                {
                    if (SharedData.sessions[i].type == iRacingTelem.eSessionType.kSessionTypeRace)
                        SharedData.resultSession = i;
                }

                if (SharedData.resultSession >= 0)
                {
                    SharedData.visible[(int)SharedData.overlayObjects.results] = true;
                }
                else
                {
                    qualifyResultsButton.IsEnabled = true;
                    practiceResultsButton.IsEnabled = true;
                    SharedData.resultPage = -1;
                }
            }
        }

        private void stateButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.sessionstate])
                SharedData.visible[(int)SharedData.overlayObjects.sessionstate] = false;
            else
                SharedData.visible[(int)SharedData.overlayObjects.sessionstate] = true;
        }

        private void flagButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.flag])
                SharedData.visible[(int)SharedData.overlayObjects.flag] = false;
            else
                SharedData.visible[(int)SharedData.overlayObjects.flag] = true;
        }

        private void StartLightsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.startlights])
                SharedData.visible[(int)SharedData.overlayObjects.startlights] = false;
            else
                SharedData.visible[(int)SharedData.overlayObjects.startlights] = true;
        }

        private void tickerButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.ticker])
                SharedData.visible[(int)SharedData.overlayObjects.ticker] = false;
            else
                SharedData.visible[(int)SharedData.overlayObjects.ticker] = true;
        }

        private void LaptimeButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.laptime])
                SharedData.visible[(int)SharedData.overlayObjects.laptime] = false;
            else
                SharedData.visible[(int)SharedData.overlayObjects.laptime] = true;
        }

        private void sidepanelButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.sidepanel])
            {
                SharedData.visible[(int)SharedData.overlayObjects.sidepanel] = false;
            }
            else
            {
                // hide results
                SharedData.visible[(int)SharedData.overlayObjects.results] = false;
                SharedData.resultSession = -1;
                SharedData.resultPage = -1;
                SharedData.resultLastPage = false;
                practiceResultsButton.IsEnabled = true;
                qualifyResultsButton.IsEnabled = true;
                raceResultsButton.IsEnabled = true;

                SharedData.visible[(int)SharedData.overlayObjects.sidepanel] = true;
            }
        }
        */
    }
}
