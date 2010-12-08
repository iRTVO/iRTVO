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
            textBoxLocation.Text = Properties.Settings.Default.OverlayLocationX + "x" + Properties.Settings.Default.OverlayLocationY;
            textBoxSize.Text = Properties.Settings.Default.OverlayWidth + "x" + Properties.Settings.Default.OverlayHeight;
        }

        private void bMove_Click(object sender, RoutedEventArgs e)
        {
            string[] coords = textBoxLocation.Text.Split('x');
            int x = -1;
            int y = -1;
            try
            {
                x = Int32.Parse(coords[0]);
                y = Int32.Parse(coords[1]);
            }
            catch (System.FormatException)
            {
                MessageBox.Show("Input is not valid location. Use notation [x]x[y]. For example: 100x200.");
            }

            if (x >= 0 && y >= 0)
            {

                Properties.Settings.Default.OverlayLocationX = x;
                Properties.Settings.Default.OverlayLocationY = y;
                Properties.Settings.Default.Save();

                overlayWindow.Left = Int32.Parse(coords[0]);
                overlayWindow.Top = Int32.Parse(coords[1]);
            }
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
                //results.Visibility = Visibility.Hidden;
                SharedData.visible[(int)SharedData.overlayObjects.results] = false;
                SharedData.resultSession = -1;
                SharedData.resultPage = -1;
                SharedData.resultLastPage = false;
                practiceResultsButton.IsEnabled = true;
                qualifyResultsButton.IsEnabled = true;
                raceResultsButton.IsEnabled = true;

                //sidepanel.Visibility = Visibility.Visible;
                SharedData.visible[(int)SharedData.overlayObjects.sidepanel] = true;
            }
        }

        private void bQuit_Click(object sender, RoutedEventArgs e)
        {
            SharedData.runApi = false;
            overlayWindow.Close();
            Application.Current.Shutdown(0);
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

        private void practiceResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SharedData.visible[(int)SharedData.overlayObjects.results] == false)
            {
                SharedData.resultPage = 0;
                qualifyResultsButton.IsEnabled = false;
                raceResultsButton.IsEnabled = false;
                //sidepanel.Visibility = Visibility.Hidden;
                SharedData.visible[(int)SharedData.overlayObjects.sidepanel] = false;
            }
            else if (SharedData.visible[(int)SharedData.overlayObjects.results] == true && SharedData.resultLastPage == true)
            {
                //results.Visibility = Visibility.Hidden;
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
                    //resultsHeaderLabel.Content = i18n.practice_results;
                    //results.Visibility = Visibility.Visible;
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
                //sidepanel.Visibility = Visibility.Hidden;
                SharedData.visible[(int)SharedData.overlayObjects.sidepanel] = false;
            }
            else if (SharedData.visible[(int)SharedData.overlayObjects.results] == true && SharedData.resultLastPage == true)
            {
                //results.Visibility = Visibility.Hidden;
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
                    //resultsHeaderLabel.Content = i18n.qualify_results;
                    //results.Visibility = Visibility.Visible;
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
                //sidepanel.Visibility = Visibility.Hidden;
                SharedData.visible[(int)SharedData.overlayObjects.sidepanel] = false;
            }
            else if (SharedData.visible[(int)SharedData.overlayObjects.results] == true && SharedData.resultLastPage == true)
            {
                //results.Visibility = Visibility.Hidden;
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
                    //resultsHeaderLabel.Content = i18n.race_results;
                    //results.Visibility = Visibility.Visible;
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
            //overlay.Fill = new SolidColorBrush(Colors.Black);
            //Refresh(overlay);

            thMacro = new Thread(macro.rewind);
            thMacro.Start(Int32.Parse(rewindTextbox.Text));
            thMacro.Join();

            //overlay.Fill = null;
            //oReplay.Visibility = System.Windows.Visibility.Visible;
        }

        private void liveButton_Click(object sender, RoutedEventArgs e)
        {
            //oReplay.Visibility = System.Windows.Visibility.Hidden;
            macro.live();
        }

        private void hideButton_Click(object sender, RoutedEventArgs e)
        {
            // results
            //results.Visibility = Visibility.Hidden;
            //SharedData.visible[(int)SharedData.overlayObjects.results] = false;

            for (int i = 0; i < SharedData.visible.Length; i++)
                SharedData.visible[i] = false;

            SharedData.resultSession = -1;
            SharedData.resultPage = -1;
            SharedData.resultLastPage = false;
            practiceResultsButton.IsEnabled = true;
            qualifyResultsButton.IsEnabled = true;
            raceResultsButton.IsEnabled = true;
            // sidepanel
            //sidepanel.Visibility = Visibility.Hidden;
            //SharedData.visible[(int)SharedData.overlayObjects.sidepanel] = false;
            // driver
            //oFollow.Visibility = System.Windows.Visibility.Hidden;
            // replay
            //oReplay.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Main_LocationChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.MainWindowLocationX = (int)this.Left;
            Properties.Settings.Default.MainWindowLocationY = (int)this.Top;
            Properties.Settings.Default.Save();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            SharedData.requestRefresh = true;
        }

        private void bResize_Click(object sender, RoutedEventArgs e)
        {
            string[] coords = textBoxSize.Text.Split('x');
            int w = -1;
            int h = -1;
            try
            {
                w = Int32.Parse(coords[0]);
                h = Int32.Parse(coords[1]);
            }
            catch (System.FormatException)
            {
                MessageBox.Show("Input is not valid location. Use notation [x]x[y]. For example: 600x480.");
            }

            if (w >= 0 && h >= 0)
            {

                Properties.Settings.Default.OverlayWidth = w;
                Properties.Settings.Default.OverlayHeight = h;
                Properties.Settings.Default.Save();

                overlayWindow.Width = Int32.Parse(coords[0]);
                overlayWindow.Height = Int32.Parse(coords[1]);
            }
        }
    }
}
