
using iRTVO.Data;
using iRTVO.Interfaces;
using iRTVO.Networking;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace iRTVO
{
    public class PlaySpeed
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    /// <summary>
    /// Interaction logic for lists.xaml
    /// </summary>
    public partial class Lists : Window
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        iRacingAPI API;
        DispatcherTimer updateTimer = new DispatcherTimer();

        Thread replayThread;

        public ObservableCollection<CameraGroup> Cameras { get; private set; }
        public List<PlaySpeed> PlaySpeeds { get; private set; }

        public Lists()
        {
            InitializeComponent();

            this.Left = SharedData.settings.ListsWindowLocationX;
            this.Top = SharedData.settings.ListsWindowLocationY;
            this.Width = SharedData.settings.ListsWindowWidth;
            this.Height = SharedData.settings.ListsWindowHeight;

            if (SharedData.settings.AlwaysOnTopLists)
                this.Topmost = true;
            else
                this.Topmost = false;
            PlaySpeeds = new List<PlaySpeed>();
            PlaySpeeds.Add(new PlaySpeed { Id = -1, Name = "Normal" });
            PlaySpeeds.Add(new PlaySpeed { Id = 1, Name = "1/2x" });
            PlaySpeeds.Add(new PlaySpeed { Id = 2, Name = "1/3x" });
            PlaySpeeds.Add(new PlaySpeed { Id = 3, Name = "1/4x" });
            PlaySpeeds.Add(new PlaySpeed { Id = 4, Name = "1/5x" });
            PlaySpeeds.Add(new PlaySpeed { Id = 5, Name = "1/6x" });
            PlaySpeeds.Add(new PlaySpeed { Id = 6, Name = "1/7x" });
            PlaySpeeds.Add(new PlaySpeed { Id = 7, Name = "1/8x" });
            PlaySpeeds.Add(new PlaySpeed { Id = 9, Name = "1/10x" });
            PlaySpeeds.Add(new PlaySpeed { Id = 15, Name = "1/16x" });

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Add additional Columns to Standings
            StandingsItem tmpItem = new StandingsItem();
            Type tmpType = tmpItem.GetType();
            
            IEnumerable<string> props = ExtractHelper.IterateProps(tmpType);
            string validProps = String.Join(" , ", props).Replace("StandingsItem.", "");
            foreach (Settings.ColumnSetting col in SharedData.settings.DriversColumns)
            {
                if (!props.Contains("StandingsItem."+col.Name))
                {
                    logger.Warn("Unkown column in optins.ini standingsgrid::columns '{0}'", col);
                    logger.Warn("Valid Columns are: {0}", validProps);
                   // continue;
                }
               
                DataGridTextColumn textColumn = new DataGridTextColumn();
                textColumn.Header = col.Header;
                textColumn.Binding = new Binding(col.Name);
                standingsGrid.Columns.Add(textColumn);
                logger.Trace("Added column '{0}'", col);
            }

            // We subscribe to Session change, to prevent staningsGrid show old data
            SharedData.Sessions.PropertyChanged += CurrentSession_PropertyChanged;
           
            BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;

            UpdateDataContext();
            BookmarksGrid.DataContext = SharedData.Bookmarks.List;
            Cameras = SharedData.Camera.Groups;

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

                int found = SharedData.SelectedSectors.FindIndex(s => s.Equals(sector));
                if (found >= 0)
                {
                    cb.IsChecked = true;
                }

                i++;
            }
        }

        // Gets or sets the CollectionViewSource
        public CollectionViewSource ViewSource { get; set; }


        private void UpdateDataContext()
        {
            logger.Trace("UpdateDataContext");
            CollectionViewSource oldSource = CollectionViewSource.GetDefaultView(standingsGrid.DataContext) as CollectionViewSource;
            this.ViewSource = new CollectionViewSource();
            ViewSource.Source = SharedData.Sessions.CurrentSession.Standings;
            
            ICollectionView pView = CollectionViewSource.GetDefaultView(this.ViewSource.Source);

            if ( (oldSource != null) && (oldSource.SortDescriptions.Count > 0))
                pView.SortDescriptions.Add(oldSource.SortDescriptions[0]);
            else
                pView.SortDescriptions.Add(new SortDescription("PositionLive", ListSortDirection.Ascending));

            var liveview = (ICollectionViewLiveShaping)CollectionViewSource.GetDefaultView(this.ViewSource.Source);
            liveview.IsLiveSorting = true;

            standingsGrid.DataContext = this.ViewSource.Source;
        }

        void BindingOperations_CollectionRegistering(object sender, CollectionRegisteringEventArgs e)
        {
            
            if ( (e.Collection is ObservableCollection<StandingsItem>) ||
                 (e.Collection is ObservableCollection<Bookmark>) )
            {
                logger.Trace("CollectionRegistering Event for {0}", e.Collection);
                BindingOperations.EnableCollectionSynchronization(e.Collection, SharedData.SharedDataLock);
            }

        }

        void CurrentSession_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CurrentSession")
            {
                logger.Trace("CurrentSession changed");
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        lock (SharedData.SharedDataLock)
                        {
                            UpdateDataContext();
                        }
                    }));
                return;
            }
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

            SharedData.SelectedSectors.Sort();

            CfgFile sectorsIni = new CfgFile(Directory.GetCurrentDirectory() + "\\sectors.ini");
            sectorsIni.setValue("Sectors", SharedData.Track.Id.ToString(), String.Join(";", SharedData.SelectedSectors),false);
            sectorsIni.Save();

        }
             
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            SharedData.settings.ListsWindowLocationX = (int)this.Left;
            SharedData.settings.ListsWindowLocationY = (int)this.Top;
            SharedData.settings.Save();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SharedData.settings.ListsWindowWidth = (int)this.Width;
            SharedData.settings.ListsWindowHeight = (int)this.Height;
            SharedData.settings.Save();
        }

        void standingsGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (standingsGrid.SelectedItem != null)
            {
                StandingsItem driver = (StandingsItem)standingsGrid.SelectedItem;
                if (iRTVOConnection.isConnected && !iRTVOConnection.isServer) 
                    iRTVOConnection.BroadcastMessage("SWITCH", padCarNum(driver.Driver.NumberPlate), -1);
                if (!iRTVOConnection.isConnected || iRTVOConnection.isServer) 
                API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, padCarNum(driver.Driver.NumberPlate), -1);
                SharedData.updateControls = true;
            }
        }

        void eventsGridDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (eventsGrid.SelectedItem != null)
            {
                
                Bookmark ev = new Bookmark((SessionEvent)eventsGrid.SelectedItem);
                ev.Rewind = this.getRewindTime();
                replayThread = new Thread(rewind);
                replayThread.Start(ev);
            }
        }

        void BookmarkPlay_Clicked(object sender, RoutedEventArgs e)
        {
            // TODO: re-enable bookmark play
            return;
            if (BookmarksGrid.SelectedItem != null)
            {
                Bookmark ev = (Bookmark)BookmarksGrid.SelectedItem;
                ev.Rewind = this.getRewindTime();
                replayThread = new Thread(rewind);
                replayThread.Start(ev);
            }

        }

        private Int32 getRewindTime()
        {
            ComboBoxItem cbi = (ComboBoxItem)Rewind.SelectedItem;
            string secstr = cbi.Content.ToString();
            return Int32.Parse(secstr.Substring(0, secstr.Length - 1));
        }

        public void rewind(Object input)
        {
            try
            {
            Bookmark ev = (Bookmark)input;

                if (ev.PlaySpeed == 0)
                    ev.PlaySpeed = SharedData.selectedPlaySpeed;

            SharedData.triggers.Push(TriggerTypes.replay);

            Int32 rewindFrames = (Int32)API.sdk.GetData("ReplayFrameNum") - (int)ev.ReplayPos - (ev.Rewind * 60);

                iRTVOConnection.BroadcastMessage("SWITCH", ev.DriverIdx, ev.CamIdx);
                iRTVOConnection.BroadcastMessage("REWIND", rewindFrames, ev.PlaySpeed);


            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, ev.DriverIdx, ev.CamIdx);
            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlayPosition, (int)iRSDKSharp.ReplayPositionModeTypes.Begin, (int)(ev.ReplayPos - (ev.Rewind * 60)));

            Int32 curpos = (Int32)API.sdk.GetData("ReplayFrameNum");
            DateTime timeout = DateTime.Now;

            // wait rewind to finish, but only 15 secs
            while(curpos != (int)(ev.ReplayPos - (ev.Rewind * 60)) && (DateTime.Now-timeout).TotalSeconds < 15)
            {
                Thread.Sleep(16);
                curpos = (Int32)API.sdk.GetData("ReplayFrameNum");
            }

                

                SetPlaySpeed(ev.PlaySpeed);

            SharedData.updateControls = true;
            }
            catch (Exception ex)
            {
                logger.Error("Exception in iRTVO.Lists:rewind");
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
            API.sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, playspeed, slomo);
        }

        private void updateGrids(object sender, EventArgs e)
        {
            int gridcount = BookmarksGrid.Items.Count;
            int bookmarkcount = SharedData.Bookmarks.List.Count;
#if sss
            if (gridcount != bookmarkcount)
            {
                lock (SharedData.SharedDataLock)
                {
                    
                   /* for (int i = gridcount; i < bookmarkcount; i++)
                {
                    BookmarksGrid.Items.Add(SharedData.Bookmarks.List[i]);
                }
                   */
                    BookmarksGrid.DataContext = SharedData.Bookmarks.List;
                    BookmarksGrid.Ite
                }
            }
#endif
            gridcount = eventsGrid.Items.Count;
            int eventcount = SharedData.Events.Count;

            if (gridcount != eventcount)
            {
                lock (SharedData.SharedDataLock)
                {
                for (int i = gridcount; i < eventcount; i++)
                {
                    eventsGrid.Items.Add(SharedData.Events[i]);
                }
            }
        }
        }

        private void Rewind_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem cbi = (ComboBoxItem)Rewind.SelectedItem;
            string secstr = cbi.Content.ToString();
            int secint = Int32.Parse(secstr.Substring(0, secstr.Length - 1));
            SharedData.replayRewind = secint;
        }

        private int padCarNum(string input)
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

        private void SaveBookmarks_Clicked(object sender, RoutedEventArgs e)
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "Bookmarks"; // Default file name
            dlg.DefaultExt = ".bml"; // Default file extension
            dlg.Filter = "iRTVO Bookmark Files (.bml)|*.bml"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = dlg.FileName;
                using (StreamWriter sw = new StreamWriter(filename, false, Encoding.UTF8))
                {
                    System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(SharedData.Bookmarks.GetType());
                    x.Serialize(sw, SharedData.Bookmarks);
                }
                MessageBox.Show("Bookmarks saved to " + filename);
            }
        }

        private void LoadBookmarks_Clicked(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            // Set filter for file extension and default file extension 

            dlg.DefaultExt = ".bml"; // Default file extension
            dlg.Filter = "iRTVO Bookmark Files (.bml)|*.bml"; // Filter files by extension
            // Display OpenFileDialog by calling ShowDialog method 

            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                using (StreamReader sw = new StreamReader(filename, Encoding.UTF8))
                {
                    System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(SharedData.Bookmarks.GetType());
                    SharedData.Bookmarks = x.Deserialize(sw) as Bookmarks;                   
                }
                MessageBox.Show( SharedData.Bookmarks.List.Count+" Bookmarks loaded from " + filename);
                BookmarksGrid.DataContext = SharedData.Bookmarks.List;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
