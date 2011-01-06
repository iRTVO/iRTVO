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

        public MainWindow()
        {
            InitializeComponent();
            // set window position
            this.Left = Properties.Settings.Default.MainWindowLocationX;
            this.Top = Properties.Settings.Default.MainWindowLocationY;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            overlayWindow.Show();

            // start statusbar
            statusBarUpdateTimer.Tick += new EventHandler(updateStatusBar);
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

            statusBarFps.Text = Math.Round(1 / SharedData.overlayFPS.TotalSeconds).ToString() + " fps";
            statusBarFps.ToolTip = string.Format("fps: {0}, effective fps: {1}",  Math.Round(1 / SharedData.overlayFPS.TotalSeconds),  Math.Round(1/SharedData.overlayEffectiveFPS.TotalSeconds));
            
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

        private void bName_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.driver])
                SharedData.visible[(int)SharedData.overlayObjects.driver] = false;
            else
                SharedData.visible[(int)SharedData.overlayObjects.driver] = true;
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

        private void replayButton_Click(object sender, RoutedEventArgs e)
        {
            SharedData.visible[(int)SharedData.overlayObjects.sessionstate] = false;
            //overlay.Fill = new SolidColorBrush(Colors.Black);
            //Refresh(overlay);

            thMacro = new Thread(macro.rewind);
            thMacro.Start(Int32.Parse(rewindTextbox.Text));
            thMacro.Join();

            //overlay.Fill = null;
            SharedData.visible[(int)SharedData.overlayObjects.replay] = true;
        }

        private void liveButton_Click(object sender, RoutedEventArgs e)
        {
            SharedData.visible[(int)SharedData.overlayObjects.replay] = false;
            macro.live();
        }

        private void hideButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < SharedData.visible.Length; i++)
                SharedData.visible[i] = false;

            SharedData.resultSession = -1;
            SharedData.resultPage = -1;
            SharedData.resultLastPage = false;
            practiceResultsButton.IsEnabled = true;
            qualifyResultsButton.IsEnabled = true;
            raceResultsButton.IsEnabled = true;
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
    }
}
