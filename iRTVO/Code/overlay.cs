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
using WpfAnimatedGif;
using NLog;
using iRTVO.Caching;
using iRTVO.Interfaces;
using iRTVO.Data;
using iRTVO.Code;

namespace iRTVO
{
    public partial class Overlay : Window
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

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
            if (!SharedData.runOverlay)
                return;
            if (SharedData.refreshTheme == true)
            {                

                loadTheme(SharedData.settings.Theme);
                SharedData.Sessions.CurrentSession.Flag = SessionFlags.invalid; // Force flags to be resend to overlay
                overlay.Left = SharedData.settings.OverlayX;
                overlay.Top = SharedData.settings.OverlayY;
                overlay.Width = SharedData.settings.OverlayW;
                overlay.Height = SharedData.settings.OverlayH;

                resizeOverlay(overlay.Width, overlay.Height);
                SharedData.refreshTheme = false;
            }

            // offline functionality hax
            if(SharedData.Sessions.SessionList.Count < 1) {
                SessionInfo dummysession = new SessionInfo();
                SharedData.Sessions.SessionList.Add(dummysession);
            }

            if (SharedData.themeCacheSessionTime != SharedData.currentSessionTime || true)
            {
                SharedData.themeDriverCache = new string[64][][];
                for (Int16 i = 0; i < 64; i++)
                    SharedData.themeDriverCache[i] = new string[4][];
                SharedData.themeSessionStateCache = new Dictionary<int, string[]>();
                SharedData.themeCacheSessionTime = SharedData.currentSessionTime;
                SharedData.cacheFrameCount++;
            }

            // do we allow retirement
            SharedData.allowRetire = true;

            if (SharedData.Sessions.SessionList.Count > 0 && (SharedData.OverlaySession < SharedData.Sessions.SessionList.Count) &&
                SharedData.Sessions.SessionList[SharedData.OverlaySession].State == SessionStates.racing &&
                (SharedData.Sessions.SessionList[SharedData.OverlaySession].LapsRemaining > 0 &&
                    SharedData.Sessions.SessionList[SharedData.OverlaySession].LapsComplete > 1)
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
            SessionInfo racesession = SharedData.Sessions.findSessionByType(SessionTypes.race);
            Double leaderpos = racesession.FindPosition(1, DataOrders.position).CurrentTrackPct;

            foreach (StandingsItem si in racesession.Standings)
            {
                if (SharedData.externalPoints.ContainsKey(si.Driver.UserId))
                {
                    if (si.Position <= SharedData.theme.pointschema.Length /*&&
                        (si.CurrentTrackPct / leaderpos) > SharedData.theme.minscoringdistance */)
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
                else if (SharedData.theme.images[i].visible != visibility2boolean[images[i].Visibility])
                {
#if DEBUG
                    logger.Trace(SharedData.theme.images[i].name);
#endif
                    if (SharedData.theme.images[i].dynamic == true)
                        loadImage(images[i], SharedData.theme.images[i]);
                    
                    images[i].Visibility = boolean2visibility[SharedData.theme.images[i].visible];
                    if (SharedData.theme.images[i].doAnimate)
                    {
                        var controller = ImageBehavior.GetAnimationController(images[i]);
                        if (controller == null)
                            continue;
                        if (SharedData.theme.images[i].visible)
                        {
                            controller.GotoFrame(0);
                            controller.Play();
                        }
                        else
                        {
                            controller.Pause();
                        }
                    }
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
                    logger.Debug("Object {0} DataSet {1} DataOrder {2} Labels {3}", SharedData.theme.objects[i].name, SharedData.theme.objects[i].dataset, SharedData.theme.objects[i].dataorder, SharedData.theme.objects[i].labels.Length);
                    switch (SharedData.theme.objects[i].dataset)
                    {
                        case DataSets.standing:
                        case DataSets.points:
                        case DataSets.pit:
                        case DataSets.driverswap:    // KJ: new dataset driverswap
                            for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++) // items
                            {
                                if (SharedData.theme.objects[i].labels[j].session != SessionTypes.none)
                                    session = SharedData.Sessions.findSessionIndexByType(SharedData.theme.objects[i].labels[j].session);
                                else
                                    session = SharedData.OverlaySession;
                                logger.Trace("Session = {0} ({1})", session, SharedData.Sessions.SessionList[session].Type);
                                for (int k = 0; k < SharedData.theme.objects[i].itemCount; k++) // drivers
                                {
                                    int driverPos = 1 + k + ((SharedData.theme.objects[i].itemCount + SharedData.theme.objects[i].skip) * SharedData.theme.objects[i].page) + SharedData.theme.objects[i].labels[j].offset + SharedData.theme.objects[i].offset;
                                    Int32 standingsCount = 0;

                                    if (SharedData.theme.objects[i].dataset == DataSets.standing)
                                    {
                                        if (SharedData.theme.objects[i].carclass == null)
                                            standingsCount = SharedData.Sessions.SessionList[session].Standings.Count;
                                        else
                                            standingsCount = SharedData.Sessions.SessionList[session].getClassCarCount(SharedData.theme.objects[i].carclass);
                                    }
                                    else if (SharedData.theme.objects[i].dataset == DataSets.points)
                                    {
                                        // KJ: experimental - we can also sort by external points without new calculation
                                        if (SharedData.theme.objects[i].dataorder == DataOrders.points)
                                            standingsCount = SharedData.externalCurrentPoints.Count;
                                        else
                                            standingsCount = SharedData.externalPoints.Count;
                                    }
                                    else if (SharedData.theme.objects[i].dataset == DataSets.pit)
                                    {

                                        standingsCount = SharedData.Sessions.SessionList[session].Standings.Count(c => c.TrackSurface == SurfaceTypes.InPitStall);
                                        logger.Debug("Pit detecteed count={0}", standingsCount);
                                    }
                                    // KJ: new dataset driverswap
                                    else if (SharedData.theme.objects[i].dataset == DataSets.driverswap)
                                    {
                                        IEnumerable<StandingsItem> query = SharedData.Sessions.CurrentSession.Standings.Where(s => s.LastDriverSwap > SharedData.currentSessionTime - 10.0);
                                        foreach (StandingsItem si in query)
                                        {
                                            standingsCount++;
                                        }
                                    }

                                    SharedData.theme.objects[i].pagecount = (int)Math.Ceiling((Double)standingsCount / (Double)SharedData.theme.objects[i].itemCount);

                                    if (SharedData.theme.objects[i].carclass != null)
                                    {
                                        if ((SharedData.theme.objects[i].page + 1) * (SharedData.theme.objects[i].itemCount + SharedData.theme.objects[i].skip) >= SharedData.Sessions.SessionList[session].getClassCarCount(SharedData.theme.objects[i].carclass) ||
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
                                        labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Visibility = System.Windows.Visibility.Visible;                                        

                                        StandingsItem driver = new StandingsItem();

                                        if (SharedData.theme.objects[i].dataset == DataSets.standing)
                                        {
                                            if (SharedData.Sessions.SessionList[session].Type != SessionTypes.race && SharedData.theme.objects[i].dataorder == DataOrders.liveposition)
                                                driver = SharedData.Sessions.SessionList[session].FindPosition(driverPos, DataOrders.position, SharedData.theme.objects[i].carclass);
                                            else
                                                driver = SharedData.Sessions.SessionList[session].FindPosition(driverPos, SharedData.theme.objects[i].dataorder, SharedData.theme.objects[i].carclass);
                                        }
                                        else if (SharedData.theme.objects[i].dataset == DataSets.points)
                                        {
                                            // KJ: experimental - sort by current points or standings before the race
                                            KeyValuePair<int, int> item;

                                            if (SharedData.theme.objects[i].dataorder == DataOrders.points)
                                                item = SharedData.externalCurrentPoints.OrderByDescending(key => key.Value).Skip(driverPos - 1).FirstOrDefault();
                                            else
                                                item = SharedData.externalPoints.OrderByDescending(key => key.Value).Skip(driverPos - 1).FirstOrDefault();

                                            driver = SharedData.Sessions.SessionList[session].Standings.SingleOrDefault(si => si.Driver.UserId == item.Key);

                                            if (driver == null)
                                            {
                                                driver = new StandingsItem();
                                                driver.Driver.UserId = item.Key;
                                                // KJ: ok, let's check if some of the data is to be overwritten by data from data.csv
                                                string[] external_driver;
                                                SharedData.externalData.TryGetValue(item.Key, out external_driver);
                                                int ed_idx;
                                                SharedData.theme.getIniValue("General", "dataFullName");
                                                if ((ed_idx = Int32.Parse(SharedData.theme.getIniValue("General", "dataFullName"))) >= 0 && external_driver.Length > ed_idx)
                                                {
                                                    // fullname gets replaced with column of data.csv
                                                    driver.Driver.Name = external_driver[ed_idx];
                                                }
                                                if ((ed_idx = Int32.Parse(SharedData.theme.getIniValue("General", "dataShortName"))) >= 0 && external_driver.Length > ed_idx)
                                                {
                                                    // shortname gets replaced with column of data.csv
                                                    driver.Driver.Shortname = external_driver[ed_idx];
                                                }
                                                if ((ed_idx = Int32.Parse(SharedData.theme.getIniValue("General", "dataInitials"))) >= 0 && external_driver.Length > ed_idx)
                                                {
                                                    // initials get replaced with column of data.csv
                                                    driver.Driver.Initials = external_driver[ed_idx];
                                                }
                                            }
                                        }
                                        else if (SharedData.theme.objects[i].dataset == DataSets.pit)
                                        {
                                            var tmpItem = from st in SharedData.Sessions.SessionList[session].Standings
                                                          where
                                                              st.TrackSurface == SurfaceTypes.InPitStall
                                                          select st;
                                            driver = tmpItem.Skip(driverPos - 1).FirstOrDefault();
                                            logger.Debug("PIT driver==null = {0}", driver);
                                            if ( driver == null )
                                                continue;

                                        }
                                        // KJ: new dataset driverswap - able to show the driverswaps in the last x (10) seconds
                                        else if (SharedData.theme.objects[i].dataset == DataSets.driverswap)
                                        {
                                            var tmpItem = from st in SharedData.Sessions.SessionList[session].Standings.OrderByDescending(s => s.LastDriverSwap)
                                                          where
                                                              st.LastDriverSwap - 10 < SharedData.currentSessionTime
                                                          select st;
                                            driver = tmpItem.Skip(driverPos - 1).FirstOrDefault();
                                            logger.Info("DRIVERSWAP driver == null = {0}", driver);
                                            if ( driver == null)
                                                continue;
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

                                            // labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = new SolidColorBrush(System.Windows.Media.Colors.Transparent);

                                            labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = SharedData.theme.DynamicBrushCache.GetDynamicBrush(filename,
                                                Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].defaultBackgroundImage, 
                                                SharedData.theme.objects[i].labels[j].backgroundColor);                                            
                                        }
                                        else 
                                        {
                                            labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = SharedData.theme.DynamicBrushCache.GetDynamicBrush( 
                                                Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].backgroundImage,
                                                SharedData.theme.objects[i].labels[j].backgroundColor);
                                        }
                                    }
                                    else 
                                    {
                                        labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Visibility = System.Windows.Visibility.Hidden;
                                        labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Content = null;
                                        labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = SharedData.theme.objects[i].labels[j].backgroundColor;
                                    }
                                }
                            }
                            break;

                        case DataSets.sessionstate:
                            for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++)
                            {
                                if (SharedData.theme.objects[i].labels[j].session != SessionTypes.none)
                                    session = SharedData.Sessions.findSessionIndexByType(SharedData.theme.objects[i].labels[j].session);
                                else
                                    session = SharedData.OverlaySession;

                                labels[i][j].Content = SharedData.theme.formatSessionstateText(
                                        SharedData.theme.objects[i].labels[j],
                                        session);
                                if (SharedData.theme.objects[i].labels[j].dynamic == true)
                                {
                                    Theme.LabelProperties label = new Theme.LabelProperties();
                                    label.text = SharedData.theme.objects[i].labels[j].backgroundImage;

                                    string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatSessionstateText(
                                        label,
                                        session);

                                    // labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Background = new SolidColorBrush(System.Windows.Media.Colors.Transparent);

                                    labels[i][j].Background = SharedData.theme.DynamicBrushCache.GetDynamicBrush(filename,
                                        Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].defaultBackgroundImage,
                                        SharedData.theme.objects[i].labels[j].backgroundColor);
                                }
                                else
                                {
                                    labels[i][j].Background = SharedData.theme.DynamicBrushCache.GetDynamicBrush(
                                        Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].backgroundImage,
                                        SharedData.theme.objects[i].labels[j].backgroundColor);
                                }
                            }
                            break;
                        default:
                        case DataSets.followed:
                        case DataSets.radio:
                        case DataSets.trigger:
                           
                            for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++)
                            {
                                if (SharedData.theme.objects[i].labels[j].session != SessionTypes.none)
                                    session = SharedData.Sessions.findSessionIndexByType(SharedData.theme.objects[i].labels[j].session);
                                else
                                    session = SharedData.OverlaySession;

                                int pos;
                                if (SharedData.theme.objects[i].dataorder == DataOrders.liveposition && SharedData.Sessions.SessionList[session].Type == SessionTypes.race)
                                    pos = SharedData.Sessions.SessionList[session].FollowedDriver.PositionLive;
                                else if (SharedData.theme.objects[i].dataorder == DataOrders.trackposition)
                                    pos = 0;
                                else
                                    pos = SharedData.Sessions.SessionList[session].FollowedDriver.Position;

                                int offset = SharedData.theme.objects[i].labels[j].offset + SharedData.theme.objects[i].offset;

                                switch (SharedData.theme.objects[i].dataset)
                                {
                                    case DataSets.trigger:
                                        labels[i][j].Content = SharedData.theme.formatFollowedText(SharedData.theme.objects[i].labels[j],
                                                        SharedData.Sessions.SessionList[session].FindDriver(SharedData.currentTriggerCarIdx),
                                                        SharedData.Sessions.SessionList[session]);
                                        break;
                                    case DataSets.radio:
                                        labels[i][j].Content = SharedData.theme.formatFollowedText(SharedData.theme.objects[i].labels[j],
                                                        SharedData.Sessions.SessionList[session].FindDriver(SharedData.currentRadioCarIdx),
                                                        SharedData.Sessions.SessionList[session]);
                                        break;
                                    default:
                                        labels[i][j].Content = SharedData.theme.formatFollowedText(SharedData.theme.objects[i].labels[j],
                                            SharedData.Sessions.SessionList[session].FindPosition(pos + offset, SharedData.theme.objects[i].dataorder),
                                            SharedData.Sessions.SessionList[session]);
                                        break;
                                }
                                

                                if (SharedData.theme.objects[i].labels[j].dynamic == true)
                                {
                                    Theme.LabelProperties label = new Theme.LabelProperties();
                                    label.text = SharedData.theme.objects[i].labels[j].backgroundImage;

                                    string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatFollowedText(
                                        label,
                                        SharedData.Sessions.SessionList[session].FindPosition(pos + offset, SharedData.theme.objects[i].dataorder),
                                        SharedData.Sessions.SessionList[session]
                                    );

                                    labels[i][j].Background = SharedData.theme.DynamicBrushCache.GetDynamicBrush(filename,
                                                Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.objects[i].labels[j].defaultBackgroundImage, 
                                                SharedData.theme.objects[i].labels[j].backgroundColor);                                            
                                                                           
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
                        case DataSets.standing:
                            if (tickerStackpanels[i].Margin.Left + tickerStackpanels[i].ActualWidth <= 0)
                            {
                                
                                // Create tickers
                                int length;
                                if (SharedData.theme.tickers[i].carclass != null)
                                    length = SharedData.Sessions.SessionList[SharedData.OverlaySession].getClassCarCount(SharedData.theme.tickers[i].carclass);
                                else
                                    length = SharedData.Sessions.SessionList[SharedData.OverlaySession].Standings.Count;
                                    
                                tickerScrolls[i].Children.Clear();
                                tickerStackpanels[i].Children.Clear();

                                tickerStackpanels[i] = new StackPanel();
                                tickerStackpanels[i].Margin = new Thickness(SharedData.theme.tickers[i].width, 0, 0, 0);
                                tickerStackpanels[i].Orientation = Orientation.Horizontal;

                                if (SharedData.theme.tickers[i].fillVertical)
                                    tickerRowpanels[i] = new StackPanel[length];

                                //tickers[i].Children.Add(tickerStackpanels[i]);
                                tickerScrolls[i].Children.Add(tickerStackpanels[i]);
                                tickerLabels[i] = new Label[SharedData.Sessions.SessionList[SharedData.OverlaySession].Standings.Count * SharedData.theme.tickers[i].labels.Length];

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
                                            SharedData.Sessions.SessionList[SharedData.OverlaySession].FindPosition(j + 1, SharedData.theme.tickers[i].dataorder, SharedData.theme.tickers[i].carclass),
                                            SharedData.Sessions.SessionList[SharedData.OverlaySession]);
                                        if (SharedData.theme.tickers[i].labels[k].width == 0)
                                            tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Width = Double.NaN;

                                        if (SharedData.theme.tickers[i].labels[k].dynamic == true)
                                        {
                                            Theme.LabelProperties label = new Theme.LabelProperties();
                                            label.text = SharedData.theme.tickers[i].labels[k].backgroundImage;

                                            string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatFollowedText(
                                                label,
                                                SharedData.Sessions.SessionList[SharedData.OverlaySession].FindPosition(j + 1, SharedData.theme.tickers[i].dataorder),
                                                SharedData.Sessions.SessionList[SharedData.OverlaySession]
                                            );

                                            tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Background = SharedData.theme.DynamicBrushCache.GetDynamicBrush(filename,
                                                Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage,
                                                SharedData.theme.tickers[i].labels[k].backgroundColor);                                            
                                                                                   
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
                                tickerStoryboards[i].Begin(this,true);
                            }
                            else
                            {
                                
                                // update data
                                
                                tickerAnimations[i].From = new Thickness(SharedData.theme.tickers[i].width + tickerStackpanels[i].ActualWidth, 0, 0, 0);
                                tickerAnimations[i].To = new Thickness(0);
                                
                                Double margin = tickerStackpanels[i].Margin.Left; // +tickerScrolls[i].Margin.Left;
                                int length;
                                if (SharedData.theme.tickers[i].carclass != null)
                                    length = SharedData.Sessions.SessionList[SharedData.OverlaySession].getClassCarCount(SharedData.theme.tickers[i].carclass);
                                else
                                    length = SharedData.Sessions.SessionList[SharedData.OverlaySession].Standings.Count;
                                
                                for (int j = 0; j < length; j++) // drivers
                                {
                                    for (int k = 0; k < SharedData.theme.tickers[i].labels.Length; k++) // labels
                                    {
                                        if ((j * SharedData.theme.tickers[i].labels.Length) + k < tickerLabels[i].Length)
                                        {
                                            // TODO: This means tickers only get updated once every repeat
                                            if (margin > (0 - tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].DesiredSize.Width) && margin <= SharedData.theme.tickers[i].width)
                                            {

                                                tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Content = SharedData.theme.formatFollowedText(
                                                    SharedData.theme.tickers[i].labels[k],
                                                    SharedData.Sessions.SessionList[SharedData.OverlaySession].FindPosition(j + 1, SharedData.theme.tickers[i].dataorder, SharedData.theme.tickers[i].carclass),
                                                    SharedData.Sessions.SessionList[SharedData.OverlaySession]);

                                                // fixing label width screwing up ticker.From
                                                if (tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Content.ToString() != "")
                                                    SharedData.tickerReady[i] = true;

                                                if (SharedData.theme.tickers[i].labels[k].dynamic == true)
                                                {
                                                    Theme.LabelProperties label = new Theme.LabelProperties();
                                                    label.text = SharedData.theme.tickers[i].labels[k].backgroundImage;

                                                    string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatFollowedText(
                                                        label,
                                                        SharedData.Sessions.SessionList[SharedData.OverlaySession].FindPosition(j + 1, SharedData.theme.tickers[i].dataorder),
                                                        SharedData.Sessions.SessionList[SharedData.OverlaySession]
                                                    );

                                                    tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Background = SharedData.theme.DynamicBrushCache.GetDynamicBrush(filename,
                                                        Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage,
                                                        SharedData.theme.tickers[i].labels[k].backgroundColor);


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
                        case DataSets.sessionstate:
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
                                        SharedData.OverlaySession);
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
                                                SharedData.OverlaySession);

                                            if (SharedData.theme.tickers[i].labels[k].dynamic == true)
                                            {
                                                Theme.LabelProperties label = new Theme.LabelProperties();
                                                label.text = SharedData.theme.tickers[i].labels[k].backgroundImage;

                                                string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatSessionstateText(
                                                    label,
                                                    SharedData.OverlaySession
                                                );

                                                tickerLabels[i][k].Background = SharedData.theme.DynamicBrushCache.GetDynamicBrush(filename,
                                                    Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage,
                                                    SharedData.theme.tickers[i].labels[k].backgroundColor);        
                                                

                                            }
                                        }
                                        margin += tickerLabels[i][k].DesiredSize.Width;
                                    }
                                }
                                /*
                                // old scroll
                                Thickness scroller = tickerStackpanels[i].Margin;
                                scroller.Left -= SharedData.settings.TickerSpeed;
                                tickerStackpanels[i].Margin = scroller;
                                    * */
                            }
                            break;
                        default:
                        case DataSets.followed:
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
                                            SharedData.Sessions.SessionList[SharedData.OverlaySession].FollowedDriver,
                                            SharedData.Sessions.SessionList[SharedData.OverlaySession]);

                                        if (SharedData.theme.tickers[i].labels[k].dynamic == true)
                                        {
                                            Theme.LabelProperties label = new Theme.LabelProperties();
                                            label.text = SharedData.theme.tickers[i].labels[k].backgroundImage;

                                            string filename = Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.formatFollowedText(
                                                label,
                                                SharedData.Sessions.SessionList[SharedData.OverlaySession].FollowedDriver,
                                                SharedData.Sessions.SessionList[SharedData.OverlaySession]
                                            );

                                            tickerLabels[i][k].Background = SharedData.theme.DynamicBrushCache.GetDynamicBrush(filename,
                                                   Directory.GetCurrentDirectory() + "\\" + SharedData.theme.path + "\\" + SharedData.theme.tickers[i].labels[k].defaultBackgroundImage,
                                                   SharedData.theme.tickers[i].labels[k].backgroundColor);        

                                        }
                                    }
                                }
                                /*
                                // old scroll
                                Thickness scroller = tickerStackpanels[i].Margin;
                                scroller.Left -= SharedData.settings.TickerSpeed;
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
                    if (SharedData.Sessions.SessionList[SharedData.overlaySession].StartLight == SessionStartLights.set)
                    {
                        if (SharedData.theme.images[i].light == Theme.lights.red)
                            images[i].Visibility = System.Windows.Visibility.Visible;
                        else
                            images[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                    else if (SharedData.Sessions.SessionList[SharedData.overlaySession].StartLight == SessionStartLights.go)
                    {
                        if (SharedData.theme.images[i].light == Theme.lights.green)
                            images[i].Visibility = System.Windows.Visibility.Visible;
                        else
                            images[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                    else if (SharedData.Sessions.SessionList[SharedData.overlaySession].StartLight == SessionStartLights.off)
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
                    if (SharedData.Sessions.SessionList[SharedData.overlaySession].State == SessionStates.racing)
                    {
                        // yellow
                        if (SharedData.Sessions.SessionList[SharedData.overlaySession].Flag == SessionFlags.yellow)
                        {
                            if (SharedData.theme.images[i].flag == Theme.flags.yellow)
                                images[i].Visibility = System.Windows.Visibility.Visible;
                            else
                                images[i].Visibility = System.Windows.Visibility.Hidden;
                        }

                        // white
                        else if (SharedData.Sessions.SessionList[SharedData.overlaySession].Flag == SessionFlags.white) 
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
                    else if (SharedData.Sessions.SessionList[SharedData.overlaySession].State == SessionStates.checkered ||
                        SharedData.Sessions.SessionList[SharedData.overlaySession].State == SessionStates.cooldown)
                    {
                        if (SharedData.theme.images[i].flag == Theme.flags.checkered)
                            images[i].Visibility = System.Windows.Visibility.Visible;
                        else
                            images[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                    // gridding & pace lap
                    else if (SharedData.Sessions.SessionList[SharedData.overlaySession].State != SessionStates.gridding ||
                        SharedData.Sessions.SessionList[SharedData.overlaySession].State != SessionStates.pacing ||
                        SharedData.Sessions.SessionList[SharedData.overlaySession].State != SessionStates.warmup)
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
                    
                    videos[i].Position = new TimeSpan(0);
                    videos[i].Volume = SharedData.theme.videos[i].volume;

                    if (SharedData.theme.videos[i].muteSimulator) 
                    {
                        VolumeMixer.SetApplicationMute("iRacingSim", true);
                    }
                    videoBoxes[i].Visibility = System.Windows.Visibility.Visible;
                    videos[i].Tag = i;
                    videos[i].Play();
                    
                    SharedData.theme.videos[i].playing = true;

                    if (SharedData.theme.videos[i].loop == true)
                    {
                        videos[i].UnloadedBehavior = MediaState.Manual;
                        videos[i].MediaEnded += new RoutedEventHandler(loopVideo);
                    }
                    else
                    {
                       
                        videos[i].UnloadedBehavior = MediaState.Close;
                        videos[i].MediaEnded += new RoutedEventHandler(VideoEnded);
                    }

                }
                else if (videos[i].NaturalDuration.HasTimeSpan 
                    && (videos[i].Position >= videos[i].NaturalDuration.TimeSpan) 
                    && (SharedData.theme.videos[i].playing == true)
                    && (SharedData.theme.videos[i].loop ==  false))
                {                    
                    SharedData.theme.videos[i].playing = false;
                    SharedData.theme.videos[i].visible = false;
                    videos[i].Stop();
                    videos[i].Close();
                    videoBoxes[i].Visibility = System.Windows.Visibility.Hidden;
                    videos[i].Visibility = boolean2visibility[SharedData.theme.videos[i].visible];
                    if (SharedData.theme.videos[i].muteSimulator)
                    {
                        VolumeMixer.SetApplicationMute("iRacingSim", false);
                    }
                }
                if (videos[i].Visibility == System.Windows.Visibility.Hidden && SharedData.theme.videos[i].playing == true)
                {
                    SharedData.theme.videos[i].playing = false;
                    SharedData.theme.videos[i].visible = false;
                    videos[i].Stop();
                    videos[i].Close();
                    videoBoxes[i].Visibility = System.Windows.Visibility.Hidden;
                    videos[i].Visibility = boolean2visibility[SharedData.theme.videos[i].visible];
                    if (SharedData.theme.videos[i].muteSimulator)
                    {
                        VolumeMixer.SetApplicationMute("iRacingSim", false);
                    }
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
                        sounds[i].Volume = SharedData.theme.sounds[i].volume;   
                        logger.Debug("Soundvolume = {0}",SharedData.theme.sounds[i].volume);
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

            // scripts
            SharedData.scripting.OverlayTick(overlay);

            SharedData.overlayUpdateTime = (Int32)(DateTime.Now - mutexLocked).TotalMilliseconds;
#if DEBUG
            logger.Trace("Overlayupdate "+SharedData.overlayUpdateTime.ToString());
#endif
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
        
        private void VideoEnded(object sender, EventArgs e)
        {
            int i;
            MediaElement me;
            me = (MediaElement)sender;
            i = Convert.ToInt32(me.Tag);
            me.Stop();
            SharedData.theme.videos[i].playing = false;
            SharedData.theme.videos[i].visible = false;
            videos[i].Stop();
            videoBoxes[i].Visibility = System.Windows.Visibility.Hidden;
            videos[i].Visibility = boolean2visibility[SharedData.theme.videos[i].visible];
            if (SharedData.theme.videos[i].muteSimulator)
            {
                VolumeMixer.SetApplicationMute("iRacingSim", false);
            }
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