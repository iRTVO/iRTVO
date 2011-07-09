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
// additional
using System.Windows.Threading;
using System.ComponentModel;

namespace iRTVO
{
    /// <summary>
    /// Interaction logic for lists.xaml
    /// </summary>
    public partial class Lists : Window
    {
        iRacingAPI API;
        DispatcherTimer updateTimer = new DispatcherTimer();

        public Lists()
        {
            InitializeComponent();

            this.Left = Properties.Settings.Default.listsWindowLocationX;
            this.Top = Properties.Settings.Default.listsWindowLocationY;
            this.Width = Properties.Settings.Default.listsWindowWidth;
            this.Height = Properties.Settings.Default.listsWindowHeight;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            standingsGrid.DataContext = SharedData.Sessions.CurrentSession.Standings;
            //eventsGrid.DataContext = SharedData.Events.List;
            //BookmarksGrid.DataContext = SharedData.Bookmarks.List;

            API = new iRTVO.iRacingAPI();
            API.sdk.Startup();

            updateTimer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            updateTimer.Tick += new EventHandler(updateGrids);
            updateTimer.Start();
            updateGrids(new object(), new EventArgs());

            int i = 0;

            foreach (Single sector in SharedData.Sectors)
            {
                CheckBox cb = new CheckBox();
                cb.Content = "Sector " + i + ": " + sector.ToString("0.000");
                cb.Name = "s" + i;
                cb.Click += new RoutedEventHandler(sectorClick);
                sectorsStackPanel.Children.Add(cb);
                i++;
            }

        }

        void sectorClick(object sender, RoutedEventArgs e)
        {
            CheckBox fe2 = (CheckBox)e.Source;
            int index = Int32.Parse(fe2.Name.Substring(1, fe2.Name.Length - 1));

            if ((Boolean)fe2.IsChecked)
            {
                SharedData.SelectedSectors.Add(SharedData.Sectors[index]);
            }
            else
            {
                SharedData.SelectedSectors.Remove(SharedData.Sectors[index]);
            }
            

        }
             
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.listsWindowLocationX = (int)this.Left;
            Properties.Settings.Default.listsWindowLocationY = (int)this.Top;
            Properties.Settings.Default.Save();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Properties.Settings.Default.listsWindowWidth = (int)this.Width;
            Properties.Settings.Default.listsWindowHeight = (int)this.Height;
            Properties.Settings.Default.Save();
        }

        void standingsGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (standingsGrid.SelectedItem != null)
            {
                StandingsItem driver = (StandingsItem)standingsGrid.SelectedItem;
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, Int32.Parse(driver.Driver.NumberPlate), -1);
                SharedData.updateControls = true;
            }
        }

        void eventsGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (eventsGrid.SelectedItem != null)
            {
                Event ev = (Event)eventsGrid.SelectedItem;
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, Int32.Parse(ev.Driver.NumberPlate), -1);
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlayPosition, 0, ev.ReplayPos);
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
                SharedData.updateControls = true;
            }
        }

        void bookmarksGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BookmarksGrid.SelectedItem != null)
            {
                Event ev = (Event)BookmarksGrid.SelectedItem;
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, Int32.Parse(ev.Driver.NumberPlate), -1);
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlayPosition, 0, ev.ReplayPos);
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 1, 0);
                SharedData.updateControls = true;
            }
        }

        private void updateGrids(object sender, EventArgs e)
        {
            int gridcount = BookmarksGrid.Items.Count;
            int bookmarkcount = SharedData.Bookmarks.List.Count;

            if (gridcount != bookmarkcount)
            {
                for (int i = gridcount; i < bookmarkcount; i++)
                {
                    BookmarksGrid.Items.Add(SharedData.Bookmarks.List[i]);
                }
            }

            gridcount = eventsGrid.Items.Count;
            int eventcount = SharedData.Events.List.Count;

            if (gridcount != eventcount)
            {
                for (int i = gridcount; i < eventcount; i++)
                {
                    eventsGrid.Items.Add(SharedData.Events.List[i]);
                }
            }
        }
    }
}
