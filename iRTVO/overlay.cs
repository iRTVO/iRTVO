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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Media.Animation;

namespace iRTVO
{
    public partial class Overlay : Window
    {

        private Dictionary<System.Windows.Visibility, Boolean> visibility2boolean = new Dictionary<System.Windows.Visibility, Boolean>(){
            {System.Windows.Visibility.Visible, true},
            {System.Windows.Visibility.Hidden, false},
            {System.Windows.Visibility.Collapsed, false}
        };

        private Dictionary<Boolean, System.Windows.Visibility> boolean2visibility = new Dictionary<Boolean, System.Windows.Visibility>(){
            {true, System.Windows.Visibility.Visible},
            {false, System.Windows.Visibility.Hidden}
        };

        // fps counter
        Stopwatch stopwatch = Stopwatch.StartNew();
        DateTime drawBegun = DateTime.Now;

        private void overlayUpdate(object sender, EventArgs e)
        {

            if (SharedData.refreshTheme == true)
            {
                loadTheme(Properties.Settings.Default.theme);

                overlay.Left = Properties.Settings.Default.OverlayLocationX;
                overlay.Top = Properties.Settings.Default.OverlayLocationY;
                overlay.Width = Properties.Settings.Default.OverlayWidth;
                overlay.Height = Properties.Settings.Default.OverlayHeight;

                resizeOverlay(overlay.Width, overlay.Height);
                SharedData.refreshTheme = false;
            }

            // offline functionality hax
            if(SharedData.Sessions.SessionList.Count < 1) {
                Sessions.SessionInfo dummysession = new Sessions.SessionInfo();
                SharedData.Sessions.SessionList.Add(dummysession);
            }

            if (SharedData.themeCacheSessionTime != SharedData.currentSessionTime || true)
            {
                SharedData.themeDriverCache = new string[64][][];
                for (Int16 i = 0; i < 64; i++)
                    SharedData.themeDriverCache[i] = new string[4][];
                SharedData.themeSessionStateCache = new string[0];
                SharedData.themeCacheSessionTime = SharedData.currentSessionTime;
                SharedData.cacheFrameCount++;
            }

            // do we allow retirement
            SharedData.allowRetire = true;

            if (SharedData.Sessions.SessionList.Count > 0 &&
                SharedData.Sessions.SessionList[SharedData.overlaySession].State == Sessions.SessionInfo.sessionState.racing &&
                (SharedData.Sessions.SessionList[SharedData.overlaySession].LapsRemaining > 0 &&
                    SharedData.Sessions.SessionList[SharedData.overlaySession].LapsComplete > 1)
                )
            {
                SharedData.allowRetire = true;
            }
            else
            {
                SharedData.allowRetire = false;
            }

            // wait
            SharedData.mutex.WaitOne();
            DateTime mutexLocked = DateTime.Now;

            // calculate points
            SharedData.externalCurrentPoints.Clear();
            Sessions.SessionInfo racesession = SharedData.Sessions.findSessionType(Sessions.SessionInfo.sessionType.race);
            Double leaderpos = racesession.FindPosition(1, dataorder.position).CurrentTrackPct;

            foreach (Sessions.SessionInfo.StandingsItem si in racesession.Standings)
            {
                if (SharedData.externalPoints.ContainsKey(si.Driver.UserId))
                {
                    if (si.Position <= SharedData.theme.pointschema.Length &&
                        (si.CurrentTrackPct / leaderpos) > SharedData.theme.minscoringdistance)
                        SharedData.externalCurrentPoints.Add(si.Driver.UserId, SharedData.externalPoints[si.Driver.UserId] + SharedData.theme.pointschema[si.Position - 1]);
                    else
                        SharedData.externalCurrentPoints.Add(si.Driver.UserId, SharedData.externalPoints[si.Driver.UserId]);
                }
            }

            foreach (KeyValuePair<int, int> driver in SharedData.externalPoints)
            {
                if (!SharedData.externalCurrentPoints.ContainsKey(driver.Key))
                {
                    SharedData.externalCurrentPoints.Add(driver.Key, SharedData.externalPoints[driver.Key]);
                }
            }

            // images
            for (int i = 0; i < images.Length; i++)
            {
                if(SharedData.theme.images[i].presistent) {
                    images[i].Visibility = System.Windows.Visibility.Visible;
                }
                else if (SharedData.theme.images[i].visible != visibility2boolean[images[i].Visibility] || SharedData.theme.images[i].dynamic == true)
                {
                    if (SharedData.theme.images[i].dynamic == true)
                        loadImage(images[i], SharedData.theme.images[i]);
                    
                    images[i].Visibility = boolean2visibility[SharedData.theme.images[i].visible];
                        
                }
            }

            // objects
            for (int i = 0; i < SharedData.theme.objects.Length; i++)
            {
                if (SharedData.theme.objects[i].presistent)
                    objects[i].Visibility = System.Windows.Visibility.Visible;
                else if (SharedData.theme.objects[i].visible != visibility2boolean[objects[i].Visibility])
                    objects[i].Visibility = boolean2visibility[SharedData.theme.objects[i].visible];

                int session;

                if (objects[i].Visibility == System.Windows.Visibility.Visible)
                {
                    switch (SharedData.theme.objects[i].dataset)
                    {
                        case Theme.dataset.standing:
                        case Theme.dataset.points:
                            for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++) // items
                            {
                                for (int k = 0; k < SharedData.theme.objects[i].itemCount; k++) // drivers
                                {
                                    int driverPos = 1 + k + ((SharedData.theme.objects[i].itemCount + SharedData.theme.objects[i].skip) * SharedData.theme.objects[i].page) + SharedData.theme.objects[i].labels[j].offset + SharedData.theme.objects[i].offset;
                                    Int32 standingsCount = 0;

                                    if (SharedData.theme.objects[i].dataset == Theme.dataset.standing)
                                    {
                                        if (SharedData.theme.objects[i].carclass == null)
                                            standingsCount = SharedData.Sessions.SessionList[SharedData.overlaySession].Standings.Count;
                                        else
                                            standingsCount = SharedData.Sessions.SessionList[SharedData.overlaySession].getClassCarCount(SharedData.theme.objects[i].carclass);
                                    }
                                    else if (SharedData.theme.objects[i].dataset == Theme.dataset.points)
                                        standingsCount = SharedData.externalCurrentPoints.Count;

                                    SharedData.theme.objects[i].pagecount = (int)Math.Ceiling((Double)standingsCount / (Double)SharedData.theme.objects[i].itemCount);

                                    if (SharedData.theme.objects[i].carclass != null)
                                    {
                                        if ((SharedData.theme.objects[i].page + 1) * (SharedData.theme.objects[i].itemCount + SharedData.theme.objects[i].skip) >= SharedData.Sessions.SessionList[SharedData.overlaySession].getClassCarCount(SharedData.theme.objects[i].carclass) ||
                                            (SharedData.theme.objects[i].maxpages > 0 && SharedData.theme.objects[i].page >= SharedData.theme.objects[i].maxpages - 1))
                                        {
                                            SharedData.lastPage[i] = true;
                                        }
                                    }
                                    else
                                    {
                                        if ((SharedData.theme.objects[i].page + 1) * (SharedData.theme.objects[i].itemCount + SharedData.theme.objects[i].skip) >= standingsCount ||
                                            (SharedData.theme.objects[i].maxpages > 0 && SharedData.theme.objects[i].page >= SharedData.theme.objects[i].maxpages - 1))
                                        {
                                            SharedData.lastPage[i] = true;
                                        }
                                    }

                                    if (driverPos <= standingsCount)
                                    {
                                        if (SharedData.theme.objects[i].labels[j].session != Theme.sessionType.none)
                                            session = SharedData.sessionTypes[SharedData.theme.objects[i].labels[j].session];
                                        else
                                            session = SharedData.overlaySession;

                                        Sessions.SessionInfo.StandingsItem driver = new Sessions.SessionInfo.StandingsItem();

                                        if (SharedData.theme.objects[i].dataset == Theme.dataset.standing)
                                        {
                                            if (SharedData.Sessions.SessionList[SharedData.overlaySession].Type != Sessions.SessionInfo.sessionType.race && SharedData.theme.objects[i].dataorder == dataorder.liveposition)
                                                driver = SharedData.Sessions.SessionList[session].FindPosition(driverPos, dataorder.position, SharedData.theme.objects[i].carclass);
                                            else
                                                driver = SharedData.Sessions.SessionList[session].FindPosition(driverPos, SharedData.theme.objects[i].dataorder, SharedData.theme.objects[i].carclass);
                                        }
                                        else if (SharedData.theme.objects[i].dataset == Theme.dataset.points)
                                        {
                                            KeyValuePair<int, int> item = SharedData.externalCurrentPoints.OrderByDescending(key => key.Value).Skip(driverPos - 1).FirstOrDefault();
                                            driver = SharedData.Sessions.SessionList[session].Standings.SingleOrDefault(si => si.Driver.UserId == item.Key);
                                            if (driver == null)
                                            {
                                                driver = new Sessions.SessionInfo.StandingsItem();
                                                driver.Driver.UserId = item.Key;
                                            }
                                        }

                                        labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Content = SharedData.theme.formatFollowedText(
                                            SharedData.theme.objects[i].labels[j],
                                            driver,
                                            SharedData.Sessions.SessionList[session]);

                                        if (SharedData.theme.objects[i].labels[j].dynamic == true)
                                        {
                                            Theme.LabelProperties label = new Theme.LabelProperties();
                                            label.text = SharedData.theme.objects[i].labels[j].backgroundImage;

                                            string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatFollowedText(
                                                label,
                                                driver,
                                                SharedData.Sessions.SessionList[session]
                                            );

                                            labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = new SolidColorBrush(System.Windows.Media.Colors.Yellow);

                                            if (File.Exists(filename))
                                            {
                                                Brush bg = new ImageBrush(new BitmapImage(new Uri(filename)));
                                                labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = bg;
                                            }
                                            else if (File.Exists(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].defaultBackgroundImage))
                                            {
                                                Brush bg = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].defaultBackgroundImage)));
                                                labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = bg;
                                            }
                                            else
                                            {
                                                labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = SharedData.theme.objects[i].labels[j].backgroundColor;
                                            }

                                        }
                                        else if (File.Exists(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].backgroundImage))
                                        {
                                            Brush bg = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].backgroundImage)));
                                            labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = bg;
                                        }
                                    }
                                    else 
                                    {
                                        labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Content = null;
                                        labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = SharedData.theme.objects[i].labels[j].backgroundColor;
                                    }
                                }
                            }
                            break;
                        case Theme.dataset.sessionstate:
                            for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++)
                            {
                                if (SharedData.theme.objects[i].labels[j].session != Theme.sessionType.none)
                                    session = SharedData.sessionTypes[SharedData.theme.objects[i].labels[j].session];
                                else
                                    session = SharedData.overlaySession;

                                labels[i][j].Content = SharedData.theme.formatSessionstateText(
                                        SharedData.theme.objects[i].labels[j],
                                        session);
                            }
                            break;
                        default:
                        case Theme.dataset.followed:
                            for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++)
                            {
                                if (SharedData.theme.objects[i].labels[j].session != Theme.sessionType.none)
                                    session = SharedData.sessionTypes[SharedData.theme.objects[i].labels[j].session];
                                else
                                    session = SharedData.overlaySession;

                                int pos;
                                if (SharedData.theme.objects[i].dataorder == dataorder.liveposition && SharedData.Sessions.SessionList[session].Type == Sessions.SessionInfo.sessionType.race)
                                    pos = SharedData.Sessions.SessionList[session].FollowedDriver.PositionLive;
                                else
                                    pos = SharedData.Sessions.SessionList[session].FollowedDriver.Position;

                                int offset = SharedData.theme.objects[i].labels[j].offset + SharedData.theme.objects[i].offset;

                                labels[i][j].Content = SharedData.theme.formatFollowedText(
                                    SharedData.theme.objects[i].labels[j],
                                    //SharedData.Sessions.SessionList[session].FindDriver(SharedData.Sessions.SessionList[session].FollowedDriver.Driver.CarIdx),
                                     SharedData.Sessions.SessionList[session].FindPosition(pos + offset, SharedData.theme.objects[i].dataorder),
                                    SharedData.Sessions.SessionList[session]);

                                if (SharedData.theme.objects[i].labels[j].dynamic == true)
                                {
                                    Theme.LabelProperties label = new Theme.LabelProperties();
                                    label.text = SharedData.theme.objects[i].labels[j].backgroundImage;

                                    string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatFollowedText(
                                        label,
                                        SharedData.Sessions.SessionList[session].FindPosition(SharedData.Sessions.SessionList[session].FollowedDriver.Position + SharedData.theme.objects[i].labels[j].offset + SharedData.theme.objects[i].offset, dataorder.position),
                                        SharedData.Sessions.SessionList[session]
                                    );

                                    if (File.Exists(filename))
                                    {
                                        Brush bg = new ImageBrush(new BitmapImage(new Uri(filename)));
                                        labels[i][j].Background = bg;
                                    }
                                    else if (File.Exists(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].defaultBackgroundImage))
                                    {
                                        Brush bg = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].defaultBackgroundImage)));
                                        labels[i][j].Background = bg;
                                    }
                                    else
                                    {
                                        labels[i][j].Background = SharedData.theme.objects[i].labels[j].backgroundColor;
                                    }
                                }
                            }
                            break;
                    }
                }
                else
                {
                    SharedData.theme.objects[i].page = -1;
                    SharedData.lastPage[i] = false;
                }
            }

            // tickers
            for (int i = 0; i < SharedData.theme.tickers.Length; i++)
            {
                if (SharedData.theme.tickers[i].presistent)
                {
                    tickers[i].Visibility = System.Windows.Visibility.Visible;
                }
                else if (SharedData.theme.tickers[i].visible != visibility2boolean[tickers[i].Visibility])
                {
                    tickers[i].Visibility = boolean2visibility[SharedData.theme.tickers[i].visible];
                    tickerStackpanels[i].Margin = new Thickness(0 - tickerStackpanels[i].ActualWidth, 0, 0, 0);
                }

                if (tickers[i].Visibility == System.Windows.Visibility.Visible)
                {
                    switch (SharedData.theme.tickers[i].dataset)
                    {
                        case Theme.dataset.standing:
                            if (tickerStackpanels[i].Margin.Left + tickerStackpanels[i].ActualWidth <= 0)
                            {
                                // Create tickers
                                int length;
                                if (SharedData.theme.tickers[i].carclass != null)
                                    length = SharedData.Sessions.SessionList[SharedData.overlaySession].getClassCarCount(SharedData.theme.tickers[i].carclass);
                                else
                                    length = SharedData.Sessions.SessionList[SharedData.overlaySession].Standings.Count;
                                    
                                tickerScrolls[i].Children.Clear();
                                tickerStackpanels[i].Children.Clear();

                                tickerStackpanels[i] = new StackPanel();
                                tickerStackpanels[i].Margin = new Thickness(SharedData.theme.tickers[i].width, 0, 0, 0);
                                tickerStackpanels[i].Orientation = Orientation.Horizontal;

                                if (SharedData.theme.tickers[i].fillVertical)
                                    tickerRowpanels[i] = new StackPanel[length];

                                //tickers[i].Children.Add(tickerStackpanels[i]);
                                tickerScrolls[i].Children.Add(tickerStackpanels[i]);
                                tickerLabels[i] = new Label[SharedData.Sessions.SessionList[SharedData.overlaySession].Standings.Count * SharedData.theme.tickers[i].labels.Length];

                                // add headers 
                                if (SharedData.theme.tickers[i].header.text != null)
                                {
                                    tickerHeaders[i] = DrawLabel(SharedData.theme.tickers[i].header);
                                    tickerHeaders[i].Content = SharedData.theme.tickers[i].header.text;
                                    tickerHeaders[i].Width = Double.NaN;
                                    tickerStackpanels[i].Children.Add(tickerHeaders[i]);
                                }

                                for (int j = 0; j < length; j++) // drivers
                                {
                                    if (SharedData.theme.tickers[i].fillVertical)
                                    {
                                        tickerRowpanels[i][j] = new StackPanel();
                                        tickerStackpanels[i].Children.Add(tickerRowpanels[i][j]);
                                    }

                                    for (int k = 0; k < SharedData.theme.tickers[i].labels.Length; k++) // labels
                                    {
                                        tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k] = DrawLabel(SharedData.theme.tickers[i].labels[k]);
                                        tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Content = SharedData.theme.formatFollowedText(
                                            SharedData.theme.tickers[i].labels[k],
                                            SharedData.Sessions.SessionList[SharedData.overlaySession].FindPosition(j + 1, SharedData.theme.tickers[i].dataorder, SharedData.theme.tickers[i].carclass),
                                            SharedData.Sessions.SessionList[SharedData.overlaySession]);
                                        if (SharedData.theme.tickers[i].labels[k].width == 0)
                                            tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Width = Double.NaN;

                                        if (SharedData.theme.tickers[i].labels[k].dynamic == true)
                                        {
                                            Theme.LabelProperties label = new Theme.LabelProperties();
                                            label.text = SharedData.theme.tickers[i].labels[k].backgroundImage;

                                            string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatFollowedText(
                                                label,
                                                SharedData.Sessions.SessionList[SharedData.overlaySession].FindPosition(j + 1, SharedData.theme.tickers[i].dataorder),
                                                SharedData.Sessions.SessionList[SharedData.overlaySession]
                                            );

                                            if (File.Exists(filename))
                                            {
                                                Brush bg = new ImageBrush(new BitmapImage(new Uri(filename)));
                                                tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Background = bg;
                                            }
                                            else if (File.Exists(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage))
                                            {
                                                Brush bg = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage)));
                                                tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Background = bg;
                                            }
                                            else
                                            {
                                                tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Background = SharedData.theme.tickers[i].labels[k].backgroundColor;
                                            }
                                        }

                                        if (SharedData.theme.tickers[i].fillVertical)
                                            tickerRowpanels[i][j].Children.Add(tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k]);
                                        else
                                            tickerStackpanels[i].Children.Add(tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k]);

                                    }
                                }
                                    
                                // add footers
                                if (SharedData.theme.tickers[i].footer.text != null)
                                {
                                    tickerFooters[i] = DrawLabel(SharedData.theme.tickers[i].footer);
                                    tickerFooters[i].Content = SharedData.theme.tickers[i].footer.text;
                                    tickerFooters[i].Width = Double.NaN;
                                    tickerStackpanels[i].Children.Add(tickerFooters[i]);
                                }

                                if(this.FindName("tickerScroll" + i) == null)
                                    this.RegisterName("tickerScroll" + i, tickerStackpanels[i]);

                                Storyboard.SetTargetName(tickerAnimations[i], "tickerScroll" + i);
                                Storyboard.SetTargetProperty(tickerAnimations[i], new PropertyPath(StackPanel.MarginProperty));
                                tickerAnimations[i].From = new Thickness(SharedData.theme.tickers[i].width + tickerStackpanels[i].ActualWidth, 0, 0, 0);
                                tickerAnimations[i].To = new Thickness(0);
                                //tickerAnimations[i].Completed += tickerCompleted;
                                tickerAnimations[i].RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever;

                                tickerStoryboards[i].Children.Clear();
                                tickerStoryboards[i].Children.Add(tickerAnimations[i]);

                                tickerScrolls[i].Margin = new Thickness(0, 0, 0, 0);


                            }
                            else if (tickerScrolls[i].Margin.Left >= 0 && SharedData.tickerReady[i])
                            {
                                tickerScrolls[i].Margin = new Thickness(0 - tickerStackpanels[i].ActualWidth, 0, 0, 0);
                                tickerAnimations[i].From = new Thickness(SharedData.theme.tickers[i].width + tickerStackpanels[i].ActualWidth, 0, 0, 0);
                                tickerAnimations[i].To = new Thickness(0);
                                tickerAnimations[i].Duration = TimeSpan.FromSeconds(tickerAnimations[i].From.Value.Left / (60 * SharedData.theme.tickers[i].speed));
                                tickerStoryboards[i].Begin(this);
                            }
                            else
                            {
                                // update data
                                tickerAnimations[i].From = new Thickness(SharedData.theme.tickers[i].width + tickerStackpanels[i].ActualWidth, 0, 0, 0);
                                tickerAnimations[i].To = new Thickness(0);
                                Double margin = tickerStackpanels[i].Margin.Left; // +tickerScrolls[i].Margin.Left;

                                int length;
                                if (SharedData.theme.tickers[i].carclass != null)
                                    length = SharedData.Sessions.SessionList[SharedData.overlaySession].getClassCarCount(SharedData.theme.tickers[i].carclass);
                                else
                                    length = SharedData.Sessions.SessionList[SharedData.overlaySession].Standings.Count;

                                for (int j = 0; j < length; j++) // drivers
                                {
                                    for (int k = 0; k < SharedData.theme.tickers[i].labels.Length; k++) // labels
                                    {
                                        if ((j * SharedData.theme.tickers[i].labels.Length) + k < tickerLabels[i].Length)
                                        {
                                            if (margin > (0 - tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].DesiredSize.Width) && margin <= SharedData.theme.tickers[i].width)
                                            {
                                                tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Content = SharedData.theme.formatFollowedText(
                                                    SharedData.theme.tickers[i].labels[k],
                                                    SharedData.Sessions.SessionList[SharedData.overlaySession].FindPosition(j + 1, SharedData.theme.tickers[i].dataorder, SharedData.theme.tickers[i].carclass),
                                                    SharedData.Sessions.SessionList[SharedData.overlaySession]);

                                                // fixing label width screwing up ticker.From
                                                if (tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Content.ToString() != "")
                                                    SharedData.tickerReady[i] = true;

                                                if (SharedData.theme.tickers[i].labels[k].dynamic == true)
                                                {
                                                    Theme.LabelProperties label = new Theme.LabelProperties();
                                                    label.text = SharedData.theme.tickers[i].labels[k].backgroundImage;

                                                    string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatFollowedText(
                                                        label,
                                                        SharedData.Sessions.SessionList[SharedData.overlaySession].FindPosition(j + 1, SharedData.theme.tickers[i].dataorder),
                                                        SharedData.Sessions.SessionList[SharedData.overlaySession]
                                                    );

                                                    if (File.Exists(filename))
                                                    {
                                                        Brush bg = new ImageBrush(new BitmapImage(new Uri(filename)));
                                                        tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Background = bg;
                                                    }
                                                    else if (File.Exists(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage))
                                                    {
                                                        Brush bg = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage)));
                                                        tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Background = bg;
                                                    }
                                                    else
                                                    {
                                                        tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Background = SharedData.theme.tickers[i].labels[k].backgroundColor;
                                                    }

                                                }
                                            }

                                            if (SharedData.theme.tickers[i].fillVertical == false)
                                            {
                                                //margin += tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].DesiredSize.Width;
                                            }
                                        }
                                    }

                                    if (SharedData.theme.tickers[i].fillVertical == true && j < tickerRowpanels[i].Length)
                                    {
                                        //margin += tickerRowpanels[i][j].DesiredSize.Width;
                                    }
                                }
                            }
                            break;
                        case Theme.dataset.sessionstate:
                            if (/*tickerScrolls[i].Margin.Left +*/ tickerStackpanels[i].ActualWidth + tickerStackpanels[i].Margin.Left <= 0)
                            {
                                // create
                                //tickerScrolls[i].Children.Clear();
                                tickerStackpanels[i].Children.Clear();

                                tickerStackpanels[i] = new StackPanel();
                                tickerStackpanels[i].Margin = new Thickness(SharedData.theme.tickers[i].width, 0, 0, 0);

                                if (SharedData.theme.tickers[i].fillVertical)
                                    tickerStackpanels[i].Orientation = Orientation.Vertical;
                                else
                                    tickerStackpanels[i].Orientation = Orientation.Horizontal;

                                //tickerScrolls[i].Children.Add(tickerStackpanels[i]);
                                tickers[i].Children.Add(tickerStackpanels[i]);
                                tickerLabels[i] = new Label[SharedData.theme.tickers[i].labels.Length];

                                // add headers 
                                if (SharedData.theme.tickers[i].header.text != null)
                                {
                                    tickerHeaders[i] = DrawLabel(SharedData.theme.tickers[i].header);
                                    tickerHeaders[i].Content = SharedData.theme.tickers[i].header.text;
                                    tickerHeaders[i].Width = Double.NaN;
                                    tickerStackpanels[i].Children.Add(tickerHeaders[i]);
                                }

                                for (int j = 0; j < SharedData.theme.tickers[i].labels.Length; j++) // drivers
                                {
                                    tickerLabels[i][j] = DrawLabel(SharedData.theme.tickers[i].labels[j]);
                                    tickerLabels[i][j].Content = SharedData.theme.formatSessionstateText(
                                        SharedData.theme.tickers[i].labels[j],
                                        SharedData.overlaySession);
                                    if (SharedData.theme.tickers[i].labels[j].width == 0)
                                        tickerLabels[i][j].Width = Double.NaN;

                                    tickerStackpanels[i].Children.Add(tickerLabels[i][j]);
                                }

                                // add footers
                                if (SharedData.theme.tickers[i].footer.text != null)
                                {
                                    tickerFooters[i] = DrawLabel(SharedData.theme.tickers[i].footer);
                                    tickerFooters[i].Content = SharedData.theme.tickers[i].footer.text;
                                    tickerFooters[i].Width = Double.NaN;
                                    tickerStackpanels[i].Children.Add(tickerFooters[i]);
                                }

                                /*
                                if (this.FindName("tickerScroll" + i) == null)
                                    this.RegisterName("tickerScroll" + i, tickerStackpanels[i]);

                                Storyboard.SetTargetName(tickerAnimations[i], "tickerScroll" + i);
                                Storyboard.SetTargetProperty(tickerAnimations[i], new PropertyPath(StackPanel.MarginProperty));
                                tickerAnimations[i].From = new Thickness(SharedData.theme.tickers[i].width + tickerStackpanels[i].ActualWidth, 0, 0, 0);
                                tickerAnimations[i].To = new Thickness(0);
                                tickerAnimations[i].RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever;

                                tickerStoryboards[i].Children.Clear();
                                tickerStoryboards[i].Children.Add(tickerAnimations[i]);

                                tickerScrolls[i].Margin = new Thickness(0, 0, 0, 0);
                                */

                            }
                            else
                            {
                                /*
                                if (tickerScrolls[i].Margin.Left == 0)
                                {
                                    tickerScrolls[i].Margin = new Thickness(0 - tickerStackpanels[i].ActualWidth, 0, 0, 0);
                                    tickerAnimations[i].From = new Thickness(SharedData.theme.tickers[i].width + tickerStackpanels[i].ActualWidth, 0, 0, 0);
                                    tickerAnimations[i].Duration = TimeSpan.FromSeconds(tickerAnimations[i].From.Value.Left / 120);
                                    tickerStoryboards[i].Begin(this);
                                }
                                */

                                // update data
                                Double margin = tickerStackpanels[i].Margin.Left; // + tickerScrolls[i].Margin.Left;
                                for (int k = 0; k < SharedData.theme.tickers[i].labels.Length; k++) // labels
                                {
                                    if (k < tickerLabels[i].Length)
                                    {
                                        if (margin > (0 - tickerLabels[i][k].DesiredSize.Width) && margin < SharedData.theme.tickers[i].width)
                                        {
                                            tickerLabels[i][k].Content = SharedData.theme.formatSessionstateText(
                                                SharedData.theme.tickers[i].labels[k],
                                                SharedData.overlaySession);

                                            if (SharedData.theme.tickers[i].labels[k].dynamic == true)
                                            {
                                                Theme.LabelProperties label = new Theme.LabelProperties();
                                                label.text = SharedData.theme.tickers[i].labels[k].backgroundImage;

                                                string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatSessionstateText(
                                                    label,
                                                    SharedData.overlaySession
                                                );

                                                if (File.Exists(filename))
                                                {
                                                    Brush bg = new ImageBrush(new BitmapImage(new Uri(filename)));
                                                    tickerLabels[i][k].Background = bg;
                                                }
                                                else if (File.Exists(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage))
                                                {
                                                    Brush bg = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage)));
                                                    tickerLabels[i][k].Background = bg;
                                                }
                                                else
                                                {
                                                    tickerLabels[i][k].Background = SharedData.theme.tickers[i].labels[k].backgroundColor;
                                                }

                                            }
                                        }
                                        margin += tickerLabels[i][k].DesiredSize.Width;
                                    }
                                }
                                /*
                                // old scroll
                                Thickness scroller = tickerStackpanels[i].Margin;
                                scroller.Left -= Properties.Settings.Default.TickerSpeed;
                                tickerStackpanels[i].Margin = scroller;
                                    * */
                            }
                            break;
                        default:
                        case Theme.dataset.followed:
                            if (tickerStackpanels[i].ActualWidth + tickerStackpanels[i].Margin.Left <= 0)
                            {
                                // create
                                //tickerScrolls[i].Children.Clear();
                                tickerStackpanels[i].Children.Clear();

                                tickerStackpanels[i] = new StackPanel();
                                tickerStackpanels[i].Margin = new Thickness(SharedData.theme.tickers[i].width, 0, 0, 0);

                                if (SharedData.theme.tickers[i].fillVertical)
                                    tickerStackpanels[i].Orientation = Orientation.Vertical;
                                else
                                    tickerStackpanels[i].Orientation = Orientation.Horizontal;

                                //tickerScrolls[i].Children.Add(tickerStackpanels[i]);
                                tickers[i].Children.Add(tickerStackpanels[i]);

                                tickerLabels[i] = new Label[SharedData.theme.tickers[i].labels.Length];

                                // add headers 
                                if (SharedData.theme.tickers[i].header.text != null)
                                {
                                    tickerHeaders[i] = DrawLabel(SharedData.theme.tickers[i].header);
                                    tickerHeaders[i].Content = SharedData.theme.tickers[i].header.text;
                                    tickerHeaders[i].Width = Double.NaN;
                                    tickerStackpanels[i].Children.Add(tickerHeaders[i]);
                                }

                                for (int j = 0; j < SharedData.theme.tickers[i].labels.Length; j++) // drivers
                                {
                                    tickerLabels[i][j] = DrawLabel(SharedData.theme.tickers[i].labels[j]);
                                    if (SharedData.theme.tickers[i].labels[j].width == 0)
                                        tickerLabels[i][j].Width = Double.NaN;

                                    tickerStackpanels[i].Children.Add(tickerLabels[i][j]);
                                }

                                // add footers
                                if (SharedData.theme.tickers[i].footer.text != null)
                                {
                                    tickerFooters[i] = DrawLabel(SharedData.theme.tickers[i].footer);
                                    tickerFooters[i].Content = SharedData.theme.tickers[i].footer.text;
                                    tickerFooters[i].Width = Double.NaN;
                                    tickerStackpanels[i].Children.Add(tickerFooters[i]);
                                }

                                /*
                                if (this.FindName("tickerScroll" + i) == null)
                                    this.RegisterName("tickerScroll" + i, tickerStackpanels[i]);

                                Storyboard.SetTargetName(tickerAnimations[i], "tickerScroll" + i);
                                Storyboard.SetTargetProperty(tickerAnimations[i], new PropertyPath(StackPanel.MarginProperty));
                                    
                                tickerAnimations[i].From = new Thickness(SharedData.theme.tickers[i].width, 0, 0, 0);
                                tickerStackpanels[i].Margin = new Thickness(SharedData.theme.tickers[i].width, 0, 0, 0);
                                tickerAnimations[i].To = new Thickness(0);
                                tickerAnimations[i].RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever;
                                tickerAnimations[i].Duration = TimeSpan.FromSeconds(tickerAnimations[i].From.Value.Left / 120);
                                    
                                tickerStoryboards[i].Children.Clear();
                                tickerStoryboards[i].Children.Add(tickerAnimations[i]);
                                //tickerStoryboards[i].Begin(this);

                                tickerScrolls[i].Margin = new Thickness(0, 0, 0, 0);
                                    * */
                            }
                            else
                            {
                                /*
                                if (tickerScrolls[i].Margin.Left == 0)
                                {
                                        
                                    tickerScrolls[i].Margin = new Thickness(0 - tickerStackpanels[i].ActualWidth, 0, 0, 0);
                                    tickerAnimations[i].From = new Thickness(SharedData.theme.tickers[i].width + tickerStackpanels[i].ActualWidth, 0, 0, 0);
                                    //tickerAnimations[i].To = new Thickness(0);
                                    //tickerAnimations[i].RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever;
                                    //tickerAnimations[i].Duration = TimeSpan.FromSeconds(tickerAnimations[i].From.Value.Left / 120);
                                    //tickerStackpanels[i].Margin = new Thickness(SharedData.theme.tickers[i].width + (tickerStackpanels[i].ActualWidth * 2), 0, 0, 0);
                                    //tickerStoryboards[i].Begin(this);
                                    //tickerStoryboards[i].Resume(this);

                                }
                                */
                                // update data
                                for (int k = 0; k < SharedData.theme.tickers[i].labels.Length; k++) // labels
                                {
                                    if (k < tickerLabels[i].Length)
                                    {
                                        tickerLabels[i][k].Content = SharedData.theme.formatFollowedText(
                                            SharedData.theme.tickers[i].labels[k],
                                            SharedData.Sessions.SessionList[SharedData.overlaySession].FollowedDriver,
                                            SharedData.Sessions.SessionList[SharedData.overlaySession]);

                                        if (SharedData.theme.tickers[i].labels[k].dynamic == true)
                                        {
                                            Theme.LabelProperties label = new Theme.LabelProperties();
                                            label.text = SharedData.theme.tickers[i].labels[k].backgroundImage;

                                            string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatFollowedText(
                                                label,
                                                SharedData.Sessions.SessionList[SharedData.overlaySession].FollowedDriver,
                                                SharedData.Sessions.SessionList[SharedData.overlaySession]
                                            );

                                            if (File.Exists(filename))
                                            {
                                                Brush bg = new ImageBrush(new BitmapImage(new Uri(filename)));
                                                tickerLabels[i][k].Background = bg;
                                            }
                                            else if (File.Exists(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage))
                                            {
                                                Brush bg = new ImageBrush(new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage)));
                                                tickerLabels[i][k].Background = bg;
                                            }
                                            else
                                            {
                                                tickerLabels[i][k].Background = SharedData.theme.tickers[i].labels[k].backgroundColor;
                                            }

                                        }
                                    }
                                }
                                /*
                                // old scroll
                                Thickness scroller = tickerStackpanels[i].Margin;
                                scroller.Left -= Properties.Settings.Default.TickerSpeed;
                                tickerStackpanels[i].Margin = scroller;
                                    * */
                            }
                            break;
                    }
                }
                    /*
                else if (tickerStackpanels[i].Margin.Left + tickerStackpanels[i].ActualWidth > 0)
                {
                    tickerStackpanels[i].Margin = new Thickness(0 - tickerStackpanels[i].ActualWidth, 0, 0, 0);
                }
                        * */
            }

            // start lights
            /*
            for (int i = 0; i < SharedData.theme.images.Length; i++)
            {
                if (SharedData.theme.images[i].light != Theme.lights.none && SharedData.theme.images[i].visible == true)
                {
                    if (SharedData.Sessions.SessionList[SharedData.overlaySession].StartLight == Sessions.SessionInfo.sessionStartLight.set)
                    {
                        if (SharedData.theme.images[i].light == Theme.lights.red)
                            images[i].Visibility = System.Windows.Visibility.Visible;
                        else
                            images[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                    else if (SharedData.Sessions.SessionList[SharedData.overlaySession].StartLight == Sessions.SessionInfo.sessionStartLight.go)
                    {
                        if (SharedData.theme.images[i].light == Theme.lights.green)
                            images[i].Visibility = System.Windows.Visibility.Visible;
                        else
                            images[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                    else if (SharedData.Sessions.SessionList[SharedData.overlaySession].StartLight == Sessions.SessionInfo.sessionStartLight.off)
                    {
                        if (SharedData.theme.images[i].light == Theme.lights.off)
                            images[i].Visibility = System.Windows.Visibility.Visible;
                        else
                            images[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                }
            }

                
            for (int i = 0; i < SharedData.theme.images.Length; i++)
            {
                // flags
                if (SharedData.theme.images[i].flag != Theme.flags.none && SharedData.theme.images[i].visible == true) 
                {
                    // race
                    if (SharedData.Sessions.SessionList[SharedData.overlaySession].State == Sessions.SessionInfo.sessionState.racing)
                    {
                        // yellow
                        if (SharedData.Sessions.SessionList[SharedData.overlaySession].Flag == Sessions.SessionInfo.sessionFlag.yellow)
                        {
                            if (SharedData.theme.images[i].flag == Theme.flags.yellow)
                                images[i].Visibility = System.Windows.Visibility.Visible;
                            else
                                images[i].Visibility = System.Windows.Visibility.Hidden;
                        }

                        // white
                        else if (SharedData.Sessions.SessionList[SharedData.overlaySession].Flag == Sessions.SessionInfo.sessionFlag.white) 
                        {
                            if(SharedData.theme.images[i].flag == Theme.flags.white)
                                images[i].Visibility = System.Windows.Visibility.Visible;
                            else
                                images[i].Visibility = System.Windows.Visibility.Hidden;
                        }
                        // green
                        else
                        {
                            if (SharedData.theme.images[i].flag == Theme.flags.green)
                                images[i].Visibility = System.Windows.Visibility.Visible;
                            else
                                images[i].Visibility = System.Windows.Visibility.Hidden;
                        }

                    }
                    // finishing
                    else if (SharedData.Sessions.SessionList[SharedData.overlaySession].State == Sessions.SessionInfo.sessionState.checkered ||
                        SharedData.Sessions.SessionList[SharedData.overlaySession].State == Sessions.SessionInfo.sessionState.cooldown)
                    {
                        if (SharedData.theme.images[i].flag == Theme.flags.checkered)
                            images[i].Visibility = System.Windows.Visibility.Visible;
                        else
                            images[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                    // gridding & pace lap
                    else if (SharedData.Sessions.SessionList[SharedData.overlaySession].State != Sessions.SessionInfo.sessionState.gridding ||
                        SharedData.Sessions.SessionList[SharedData.overlaySession].State != Sessions.SessionInfo.sessionState.pacing ||
                        SharedData.Sessions.SessionList[SharedData.overlaySession].State != Sessions.SessionInfo.sessionState.warmup)
                    {
                        if (SharedData.theme.images[i].flag == Theme.flags.yellow)
                            images[i].Visibility = System.Windows.Visibility.Visible;
                        else
                            images[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                    else
                        images[i].Visibility = System.Windows.Visibility.Hidden;
                }
                    
                // replay transition
                if (SharedData.theme.images[i].replay == true)
                {
                    if (SharedData.replayInProgress)
                        images[i].Visibility = System.Windows.Visibility.Visible;
                    else
                        images[i].Visibility = System.Windows.Visibility.Hidden;
                }
            }
            */
            // videos
            for (int i = 0; i < videos.Length; i++)
            {
                if (SharedData.theme.videos[i].visible != visibility2boolean[videos[i].Visibility])
                    videos[i].Visibility = boolean2visibility[SharedData.theme.videos[i].visible];

                if (videos[i].Visibility == System.Windows.Visibility.Visible && SharedData.theme.videos[i].playing == false)
                {
                    videoBoxes[i].Visibility = System.Windows.Visibility.Visible;

                    videos[i].Position = new TimeSpan(0);
                    videos[i].Play();

                    SharedData.theme.videos[i].playing = true;

                    if (SharedData.theme.videos[i].loop == true)
                    {
                        videos[i].UnloadedBehavior = MediaState.Manual;
                        videos[i].MediaEnded += new RoutedEventHandler(loopVideo);
                    }
                    else
                        videos[i].UnloadedBehavior = MediaState.Close;
                }
                else if (videos[i].NaturalDuration.HasTimeSpan && videos[i].Position >= videos[i].NaturalDuration.TimeSpan && SharedData.theme.videos[i].playing == true)
                {
                    SharedData.theme.videos[i].playing = false;
                    SharedData.theme.videos[i].visible = false;
                    videoBoxes[i].Visibility = System.Windows.Visibility.Hidden;
                    videos[i].Visibility = boolean2visibility[SharedData.theme.videos[i].visible];
                }
            }

            // sounds
            for (int i = 0; i < sounds.Length; i++)
            {
                if (SharedData.theme.sounds[i].playing == true)
                {

                    // start
                    if (sounds[i].Position == new TimeSpan(0))
                    {
                        sounds[i].Position = new TimeSpan(0);
                        sounds[i].Play();
                            
                        if (SharedData.theme.sounds[i].loop == true)
                        {
                            sounds[i].MediaEnded += new EventHandler(loopSound);
                        }
                    }
                    // stop
                    else if (sounds[i].NaturalDuration.HasTimeSpan && sounds[i].Position >= sounds[i].NaturalDuration.TimeSpan)
                    {
                        SharedData.theme.sounds[i].playing = false;
                    }
                }
                else
                {
                    if(sounds[i].Position > new TimeSpan(0))
                        sounds[i].Stop();
                }
            }

            SharedData.overlayUpdateTime = (Int32)(DateTime.Now - mutexLocked).TotalMilliseconds;
            SharedData.mutex.ReleaseMutex();
            
        }

        void loopSound(object sender, EventArgs e)
        {
            MediaPlayer mp = (MediaPlayer)sender;
            //mp.Stop();
            mp.Position = new TimeSpan(0);
            mp.Play();
        }

        private void scrollTickers(object sender, EventArgs e)
        {
            for (int i = 0; i < SharedData.theme.tickers.Length; i++)
            {
                if (tickers[i].Visibility == System.Windows.Visibility.Visible)
                {
                            Thickness tickerscroller = tickerStackpanels[i].Margin;
                            tickerscroller.Left -= SharedData.theme.tickers[i].speed;
                            tickerStackpanels[i].Margin = tickerscroller;
                }
                else if (tickerStackpanels[i].Margin.Left + tickerStackpanels[i].ActualWidth > 0)
                {
                    tickerStackpanels[i].Margin = new Thickness(0 - tickerStackpanels[i].ActualWidth, 0, 0, 0);
                }
            }
        }

        private void loopVideo(object sender, EventArgs e)
        {
            MediaElement me;
            me = (MediaElement)sender;
            //me.Stop(); // let's try other way
            me.Position = new TimeSpan(0);
            me.Play();
        }

        public static string floatTime2String(Single time, Int32 showMilli, Boolean showMinutes)
        {
            time = Math.Abs(time);

            int hours = (int)Math.Floor(time / 3600);
            int minutes = (int)Math.Floor((time - (hours * 3600)) / 60);
            Double seconds = Math.Floor(time % 60);
            Double milliseconds = Math.Round(time * 1000 % 1000, 3);
            string output;

            if (time == 0.0)
                output = "-.--";
            else if (hours > 0)
            {
                output = String.Format("{0}:{1:00}:{2:00}", hours, minutes, seconds);
            }
            else if (minutes > 0 || showMinutes)
            {
                if (showMilli > 0)
                    output = String.Format("{0}:{1:00." + "".PadLeft(showMilli, '0') + "}", minutes, seconds + milliseconds / 1000);
                else
                    output = String.Format("{0}:{1:00}", minutes, seconds);
            }

            else
            {
                if (showMilli > 0)
                    output = String.Format("{0:0." + "".PadLeft(showMilli, '0') + "}", seconds + milliseconds / 1000);
                else
                    output = String.Format("{0}", seconds);
            }

            return output;
        }

        /*
        void tickerCompleted(object sender, EventArgs e)
        {
            AnimationClock anim = (AnimationClock)sender;
            //anim.From = new Thickness(SharedData.theme.tickers[i].width + tickerStackpanels[i].ActualWidth, 0, 0, 0);
            anim.
        }
        */
    }
}