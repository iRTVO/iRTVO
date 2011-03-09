/*
 * overlay.cs
 * 
 * The actual labels and canvases that are shown on the overlay are defined here.
 * 
 * overlayUpdate() updates the data on the overlay.
 * 
 * floatTime2string() converts float seconds to more familiar minute:second form.
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
using System.Diagnostics;
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

        // start lights
        TimeSpan timer;

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

            if (SharedData.runOverlay)
            {

                // wait
                SharedData.driversMutex.WaitOne(updateMs);
                SharedData.standingMutex.WaitOne(updateMs);
                SharedData.sessionsMutex.WaitOne(updateMs);

                // fps counter
                stopwatch.Restart();
                SharedData.overlayFPSstack.Push((float)(DateTime.Now - drawBegun).TotalMilliseconds);
                drawBegun = DateTime.Now;

                // do we allow retirement
                SharedData.allowRetire = true;

                /*
                if (SharedData.overlaySession >= 0)
                {
                    if (SharedData.sessions[SharedData.overlaySession].lapsRemaining <= 0 ||
                            SharedData.sessions[SharedData.overlaySession].state == iRacingTelem.eSessionState.kSessionStateCheckered)
                        SharedData.allowRetire = false;
                    else
                        SharedData.allowRetire = true;
                }
                else if (SharedData.sessions[SharedData.currentSession].lapsRemaining <= 0 ||
                        SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateCheckered)
                    SharedData.allowRetire = false;
                else
                    SharedData.allowRetire = true;
                */

                if (SharedData.sessions[SharedData.overlaySession].state == iRacingTelem.eSessionState.kSessionStateRacing &&
                        (SharedData.sessions[SharedData.overlaySession].lapsRemaining > 0 && 
                        (SharedData.sessions[SharedData.overlaySession].laps - SharedData.sessions[SharedData.overlaySession].lapsRemaining) > 1)
                    )
                {
                    SharedData.allowRetire = true;
                }
                else
                {
                    SharedData.allowRetire = false;
                }

                // images
                for (int i = 0; i < images.Length; i++)
                {
                    if (SharedData.theme.images[i].visible != visibility2boolean[images[i].Visibility])
                        images[i].Visibility = boolean2visibility[SharedData.theme.images[i].visible];
                }

                // objects
                for (int i = 0; i < SharedData.theme.objects.Length; i++)
                {
                    if (SharedData.theme.objects[i].visible != visibility2boolean[objects[i].Visibility])
                        objects[i].Visibility = boolean2visibility[SharedData.theme.objects[i].visible];

                    if (objects[i].Visibility == System.Windows.Visibility.Visible)
                    {
                        switch (SharedData.theme.objects[i].dataset)
                        {
                            case Theme.dataset.standing:
                                for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++) // items
                                {
                                    for (int k = 0; k < SharedData.theme.objects[i].itemCount; k++) // drivers
                                    {
                                        if (SharedData.theme.objects[i].itemCount * (SharedData.theme.objects[i].page + 1) >= SharedData.standing[SharedData.overlaySession].Length)
                                        {
                                            SharedData.lastPage[i] = true;
                                        }
                                        
                                        if ((k + (SharedData.theme.objects[i].itemCount * SharedData.theme.objects[i].page)) < SharedData.standing[SharedData.overlaySession].Length)
                                        {
                                            labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Content = SharedData.theme.formatFollowedText(
                                                SharedData.theme.objects[i].labels[j],
                                                SharedData.standing[SharedData.overlaySession][k + (SharedData.theme.objects[i].itemCount * SharedData.theme.objects[i].page)].id,
                                                SharedData.overlaySession);
                                        }
                                        else
                                        {
                                            labels[i][(j * SharedData.theme.objects[i].itemCount) + k].Content = null;
                                        }
                                    }
                                }
                                break;
                            case Theme.dataset.sessionstate:
                                for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++)
                                {
                                    labels[i][j].Content = SharedData.theme.formatSessionstateText(
                                         SharedData.theme.objects[i].labels[j],
                                         SharedData.overlaySession);
                                }
                                break;
                            default:
                            case Theme.dataset.followed:
                                for (int j = 0; j < SharedData.theme.objects[i].labels.Length; j++)
                                {
                                    labels[i][j].Content = SharedData.theme.formatFollowedText(
                                        SharedData.theme.objects[i].labels[j],
                                        SharedData.sessions[SharedData.overlaySession].driverFollowed,
                                        SharedData.overlaySession);
                                }
                                break;
                        }
                    }
                }

                // tickers
                for (int i = 0; i < SharedData.theme.tickers.Length; i++)
                {
                    if (SharedData.theme.tickers[i].visible != visibility2boolean[tickers[i].Visibility])
                        tickers[i].Visibility = boolean2visibility[SharedData.theme.tickers[i].visible];

                    if (tickers[i].Visibility == System.Windows.Visibility.Visible)
                    {
                        switch (SharedData.theme.tickers[i].dataset)
                        {
                            case Theme.dataset.standing:
                                if (tickerStackpanels[i].Margin.Left + tickerStackpanels[i].ActualWidth <= 0)
                                {
                                    //tickerScrolls[i].Children.Clear();
                                    tickerStackpanels[i].Children.Clear();

                                    tickerStackpanels[i] = new StackPanel();
                                    tickerStackpanels[i].Margin = new Thickness(SharedData.theme.tickers[i].width, 0, 0, 0);
                                    tickerStackpanels[i].Orientation = Orientation.Horizontal;

                                    if (SharedData.theme.tickers[i].fillVertical)
                                        tickerRowpanels[i] = new StackPanel[SharedData.standing[SharedData.overlaySession].Length];

                                    tickers[i].Children.Add(tickerStackpanels[i]);
                                    //tickerScrolls[i].Children.Add(tickerStackpanels[i]);
                                    tickerLabels[i] = new Label[SharedData.standing[SharedData.overlaySession].Length * SharedData.theme.tickers[i].labels.Length];

                                    for (int j = 0; j < SharedData.standing[SharedData.overlaySession].Length; j++) // drivers
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
                                                SharedData.standing[SharedData.overlaySession][j].id,
                                                SharedData.overlaySession);
                                            if(SharedData.theme.tickers[i].labels[k].width == 0)
                                                tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Width = Double.NaN;

                                            if (SharedData.theme.tickers[i].fillVertical)
                                                tickerRowpanels[i][j].Children.Add(tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k]);
                                            else
                                                tickerStackpanels[i].Children.Add(tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k]);
                                        }
                                    }

                                    /*
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
                                    */

                                }
                                else {
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
                                    Double margin = tickerStackpanels[i].Margin.Left; // +tickerScrolls[i].Margin.Left;
                                    for (int j = 0; j < SharedData.standing[SharedData.overlaySession].Length; j++) // drivers
                                    {
                                        for (int k = 0; k < SharedData.theme.tickers[i].labels.Length; k++) // labels
                                        {
                                            if ((j * SharedData.theme.tickers[i].labels.Length) + k < tickerLabels[i].Length)
                                            {
                                                if (margin > (0 - tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].DesiredSize.Width) && margin < SharedData.theme.tickers[i].width)
                                                {
                                                    tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].Content = SharedData.theme.formatFollowedText(
                                                        SharedData.theme.tickers[i].labels[k],
                                                        SharedData.standing[SharedData.overlaySession][j].id,
                                                        SharedData.overlaySession);
                                                }

                                                if (SharedData.theme.tickers[i].fillVertical == false)
                                                {
                                                    margin += tickerLabels[i][(j * SharedData.theme.tickers[i].labels.Length) + k].DesiredSize.Width;
                                                }
                                            }
                                        }

                                        if (SharedData.theme.tickers[i].fillVertical == true)
                                        {
                                            margin += tickerRowpanels[i][j].DesiredSize.Width;
                                        }
                                    }

                                    // old scroll
                                    Thickness scroller = tickerStackpanels[i].Margin;
                                    scroller.Left -= Properties.Settings.Default.TickerSpeed;
                                    tickerStackpanels[i].Margin = scroller;
                                }
                                break;
                            case Theme.dataset.sessionstate:
                                if (/*tickerScrolls[i].Margin.Left +*/ tickerStackpanels[i].ActualWidth + tickerStackpanels[i].Margin.Left <= 0)
                                {
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
                                            }
                                            margin += tickerLabels[i][k].DesiredSize.Width;
                                        }
                                    }

                                    // old scroll
                                    Thickness scroller = tickerStackpanels[i].Margin;
                                    scroller.Left -= Properties.Settings.Default.TickerSpeed;
                                    tickerStackpanels[i].Margin = scroller;
                                }
                                break;
                            default:
                            case Theme.dataset.followed:
                                if (tickerStackpanels[i].ActualWidth + tickerStackpanels[i].Margin.Left <= 0)
                                {
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

                                    for (int j = 0; j < SharedData.theme.tickers[i].labels.Length; j++) // drivers
                                    {
                                        tickerLabels[i][j] = DrawLabel(SharedData.theme.tickers[i].labels[j]);
                                        if (SharedData.theme.tickers[i].labels[j].width == 0)
                                            tickerLabels[i][j].Width = Double.NaN;

                                        tickerStackpanels[i].Children.Add(tickerLabels[i][j]);
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
                                                SharedData.sessions[SharedData.overlaySession].driverFollowed,
                                                SharedData.overlaySession);
                                        }
                                    }

                                    // old scroll
                                    Thickness scroller = tickerStackpanels[i].Margin;
                                    scroller.Left -= Properties.Settings.Default.TickerSpeed;
                                    tickerStackpanels[i].Margin = scroller;
                                }
                                break;
                        }
                    }
                }

                // start lights
                for (int i = 0; i < SharedData.theme.images.Length; i++)
                {
                    if (SharedData.theme.images[i].light != Theme.lights.none && SharedData.theme.images[i].visible == true)
                    {
                        if (SharedData.sessions[SharedData.overlaySession].state == iRacingTelem.eSessionState.kSessionStateWarmup ||
                        SharedData.sessions[SharedData.overlaySession].state == iRacingTelem.eSessionState.kSessionStateRacing)
                        {
                            timer = (DateTime.Now - SharedData.startlights);

                            if ((SharedData.sessions[SharedData.overlaySession].laps - SharedData.sessions[SharedData.overlaySession].lapsRemaining) < 0)
                            {
                                if (timer.TotalMinutes > 1) // reset
                                    SharedData.startlights = DateTime.Now;
                            }

                            if (timer.TotalSeconds < 5 || timer.TotalMinutes > 1)
                            {
                                if(SharedData.theme.images[i].light == Theme.lights.off)
                                    images[i].Visibility = System.Windows.Visibility.Visible;
                                else
                                    images[i].Visibility = System.Windows.Visibility.Hidden;
                            }
                            else if (SharedData.sessions[SharedData.overlaySession].state == iRacingTelem.eSessionState.kSessionStateRacing)
                            {
                                if (SharedData.theme.images[i].light == Theme.lights.green)
                                    images[i].Visibility = System.Windows.Visibility.Visible;
                                else
                                    images[i].Visibility = System.Windows.Visibility.Hidden;
                            }
                            else
                            {
                                if (SharedData.theme.images[i].light == Theme.lights.red)
                                    images[i].Visibility = System.Windows.Visibility.Visible;
                                else
                                    images[i].Visibility = System.Windows.Visibility.Hidden;
                            }
                        }
                    }
                }

                // flags
                for (int i = 0; i < SharedData.theme.images.Length; i++)
                {
                    if (SharedData.theme.images[i].flag != Theme.flags.none && SharedData.theme.images[i].visible == true) 
                    {
                        if (SharedData.sessions[SharedData.overlaySession].state == iRacingTelem.eSessionState.kSessionStateRacing)
                        {
                            if (SharedData.sessions[SharedData.overlaySession].flag == iRacingTelem.eSessionFlag.kFlagYellow && SharedData.theme.images[i].flag == Theme.flags.yellow)
                                images[i].Visibility = System.Windows.Visibility.Visible;
                            else if (SharedData.sessions[SharedData.overlaySession].lapsRemaining == 1 && SharedData.theme.images[i].flag == Theme.flags.white)
                                images[i].Visibility = System.Windows.Visibility.Visible;
                            else if (SharedData.sessions[SharedData.overlaySession].lapsRemaining <= 0 && SharedData.theme.images[i].flag == Theme.flags.checkered)
                                images[i].Visibility = System.Windows.Visibility.Visible;
                            else if (SharedData.theme.images[i].flag == Theme.flags.green)
                                images[i].Visibility = System.Windows.Visibility.Visible;
                            else
                                images[i].Visibility = System.Windows.Visibility.Hidden;
                        }
                        else if ((SharedData.sessions[SharedData.overlaySession].state == iRacingTelem.eSessionState.kSessionStateCheckered ||
                            SharedData.sessions[SharedData.overlaySession].state == iRacingTelem.eSessionState.kSessionStateCoolDown) &&
                            (SharedData.sessions[SharedData.overlaySession].state != iRacingTelem.eSessionState.kSessionStateGetInCar ||
                            SharedData.sessions[SharedData.overlaySession].state != iRacingTelem.eSessionState.kSessionStateParadeLaps ||
                            SharedData.sessions[SharedData.overlaySession].state != iRacingTelem.eSessionState.kSessionStateWarmup) &&
                            SharedData.theme.images[i].flag == Theme.flags.checkered)
                            images[i].Visibility = System.Windows.Visibility.Visible;
                        else
                            images[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                }

                /*
                // hide/show objects
                // driver
                if (SharedData.visible[(int)SharedData.overlayObjects.driver])
                {
                    if (themeImages[(int)Theme.overlayTypes.driver].Visibility == System.Windows.Visibility.Hidden)
                    {
                        if (themeImages[(int)Theme.overlayTypes.driver] != null)
                            themeImages[(int)Theme.overlayTypes.driver].Visibility = System.Windows.Visibility.Visible;
                        driver.Visibility = System.Windows.Visibility.Visible;
                    }
                }
                else
                {
                    if (themeImages[(int)Theme.overlayTypes.driver].Visibility == System.Windows.Visibility.Visible)
                    {
                        if (themeImages[(int)Theme.overlayTypes.driver] != null)
                            themeImages[(int)Theme.overlayTypes.driver].Visibility = System.Windows.Visibility.Hidden;
                        driver.Visibility = System.Windows.Visibility.Hidden;
                    }
                }

                // sidepanel
                if (SharedData.visible[(int)SharedData.overlayObjects.sidepanel])
                {
                    if (themeImages[(int)Theme.overlayTypes.sidepanel].Visibility == System.Windows.Visibility.Hidden)
                    {
                        if (themeImages[(int)Theme.overlayTypes.sidepanel] != null)
                            themeImages[(int)Theme.overlayTypes.sidepanel].Visibility = System.Windows.Visibility.Visible;
                        sidepanel.Visibility = System.Windows.Visibility.Visible;
                    }
                }
                else
                {
                    if (themeImages[(int)Theme.overlayTypes.sidepanel].Visibility == System.Windows.Visibility.Visible)
                    {
                        if (themeImages[(int)Theme.overlayTypes.sidepanel] != null)
                            themeImages[(int)Theme.overlayTypes.sidepanel].Visibility = System.Windows.Visibility.Hidden;
                        sidepanel.Visibility = System.Windows.Visibility.Hidden;
                    }
                }

                // replay
                if (SharedData.visible[(int)SharedData.overlayObjects.replay])
                {
                    if (themeImages[(int)Theme.overlayTypes.replay].Visibility == System.Windows.Visibility.Hidden)
                        themeImages[(int)Theme.overlayTypes.replay].Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    if (themeImages[(int)Theme.overlayTypes.replay].Visibility == System.Windows.Visibility.Visible)
                        themeImages[(int)Theme.overlayTypes.replay].Visibility = System.Windows.Visibility.Hidden;
                }

                // results
                if (SharedData.visible[(int)SharedData.overlayObjects.results])
                {
                    if (themeImages[(int)Theme.overlayTypes.results].Visibility == System.Windows.Visibility.Hidden)
                    {
                        if (themeImages[(int)Theme.overlayTypes.results] != null)
                            themeImages[(int)Theme.overlayTypes.results].Visibility = System.Windows.Visibility.Visible;
                        results.Visibility = System.Windows.Visibility.Visible;
                    }
                }
                else
                {
                    if (themeImages[(int)Theme.overlayTypes.results].Visibility == System.Windows.Visibility.Visible)
                    {
                        if (themeImages[(int)Theme.overlayTypes.results] != null)
                            themeImages[(int)Theme.overlayTypes.results].Visibility = System.Windows.Visibility.Hidden;
                        results.Visibility = System.Windows.Visibility.Hidden;
                    }
                }

                // session state
                if (SharedData.visible[(int)SharedData.overlayObjects.sessionstate])
                {
                    if (themeImages[(int)Theme.overlayTypes.sessionstate].Visibility == System.Windows.Visibility.Hidden)
                    {
                        if (themeImages[(int)Theme.overlayTypes.sessionstate] != null)
                            themeImages[(int)Theme.overlayTypes.sessionstate].Visibility = System.Windows.Visibility.Visible;
                        sessionstateText.Visibility = System.Windows.Visibility.Visible;
                    }
                }
                else
                {
                    if (themeImages[(int)Theme.overlayTypes.sessionstate].Visibility == System.Windows.Visibility.Visible)
                    {
                        if (themeImages[(int)Theme.overlayTypes.sessionstate] != null)
                            themeImages[(int)Theme.overlayTypes.sessionstate].Visibility = System.Windows.Visibility.Hidden;
                        sessionstateText.Visibility = System.Windows.Visibility.Hidden;
                    }
                }

                // start lights
                if (!SharedData.visible[(int)SharedData.overlayObjects.startlights])
                {
                    if (themeImages[(int)Theme.overlayTypes.lightsgreen] != null)
                        themeImages[(int)Theme.overlayTypes.lightsgreen].Visibility = System.Windows.Visibility.Hidden;
                    if (themeImages[(int)Theme.overlayTypes.lightsoff] != null)
                        themeImages[(int)Theme.overlayTypes.lightsoff].Visibility = System.Windows.Visibility.Hidden;
                    if (themeImages[(int)Theme.overlayTypes.lightsred] != null)
                        themeImages[(int)Theme.overlayTypes.lightsred].Visibility = System.Windows.Visibility.Hidden;
                }

                // ticker
                if (SharedData.visible[(int)SharedData.overlayObjects.ticker])
                {
                    if (themeImages[(int)Theme.overlayTypes.ticker].Visibility == System.Windows.Visibility.Hidden)
                    {
                        if (themeImages[(int)Theme.overlayTypes.ticker] != null)
                            themeImages[(int)Theme.overlayTypes.ticker].Visibility = System.Windows.Visibility.Visible;
                    }
                    if (ticker.Visibility == System.Windows.Visibility.Hidden)
                    {
                        ticker.Visibility = System.Windows.Visibility.Visible;

                        if (ticker.Margin.Left != theme.width)
                        {
                            // move ticker for new scroll
                            Thickness scroller = ticker.Margin;
                            scroller.Left = theme.ticker.width + 1;
                            ticker.Margin = scroller;
                        }
                    }
                    //}
                }
                else
                {

                    if (themeImages[(int)Theme.overlayTypes.ticker].Visibility == System.Windows.Visibility.Visible)
                    {
                        if (themeImages[(int)Theme.overlayTypes.ticker] != null)
                            themeImages[(int)Theme.overlayTypes.ticker].Visibility = System.Windows.Visibility.Hidden;
                    }

                    if (ticker.Visibility == System.Windows.Visibility.Visible)
                        ticker.Visibility = System.Windows.Visibility.Hidden;
                }

                // lap time
                if (SharedData.visible[(int)SharedData.overlayObjects.laptime])
                {
                    if (themeImages[(int)Theme.overlayTypes.laptime].Visibility == System.Windows.Visibility.Hidden)
                    {
                        if (themeImages[(int)Theme.overlayTypes.laptime] != null)
                            themeImages[(int)Theme.overlayTypes.laptime].Visibility = System.Windows.Visibility.Visible;
                        laptimeText.Visibility = System.Windows.Visibility.Visible;
                    }
                }
                else
                {
                    if (themeImages[(int)Theme.overlayTypes.laptime].Visibility == System.Windows.Visibility.Visible)
                    {
                        if (themeImages[(int)Theme.overlayTypes.laptime] != null)
                            themeImages[(int)Theme.overlayTypes.laptime].Visibility = System.Windows.Visibility.Hidden;
                        laptimeText.Visibility = System.Windows.Visibility.Hidden;
                    }
                }

                // do we allow retirement
                Boolean allowRetire = true;

                if (SharedData.resultSession >= 0)
                {
                    if (SharedData.sessions[SharedData.resultSession].lapsRemaining <= 0 || 
                            SharedData.sessions[SharedData.resultSession].state == iRacingTelem.eSessionState.kSessionStateCheckered)
                        allowRetire = false;
                    else
                        allowRetire = true;
                }
                else if (SharedData.sessions[SharedData.currentSession].lapsRemaining <= 0 || 
                        SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateCheckered)
                    allowRetire = false;
                else
                    allowRetire = true;


                //  driver
                if (SharedData.visible[(int)SharedData.overlayObjects.driver])
                {
                    Boolean noLapsDriver = true;
                    driverNameLabel.Content = theme.formatText(theme.driver.Name.text, SharedData.sessions[SharedData.currentSession].driverFollowed, SharedData.currentSession); // String.Format(theme.driver.Name.text, theme.getFormats(SharedData.drivers[SharedData.sessions[SharedData.currentSession].driverFollowed]));
                    driverInfoLabel.Content = theme.formatText(theme.driver.Info.text, SharedData.sessions[SharedData.currentSession].driverFollowed, SharedData.currentSession); //String.Format(theme.driver.Info.text, theme.getFormats(SharedData.drivers[SharedData.sessions[SharedData.currentSession].driverFollowed]));
                    if (SharedData.standing[SharedData.currentSession] != null)
                    {
                        for (int i = 0; i < SharedData.standing[SharedData.currentSession].Length; i++)
                        {
                            // update  driver
                            if (SharedData.standing[SharedData.currentSession][i].id == SharedData.sessions[SharedData.currentSession].driverFollowed)
                            {
                                noLapsDriver = false;
                                driverPosLabel.Content = theme.formatText(theme.driver.Num.text, SharedData.sessions[SharedData.currentSession].driverFollowed, SharedData.currentSession); //String.Format(theme.driver.Num.text, theme.getFormats(SharedData.drivers[SharedData.sessions[SharedData.currentSession].driverFollowed], i));

                                // race
                                if (SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace)
                                {
                                    if (SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id].onTrack == false && allowRetire) // out
                                    {
                                        if ((DateTime.Now - SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id].offTrackSince).TotalMilliseconds > 1000)
                                            driverDiffLabel.Content = theme.translation["out"];
                                    }
                                    else if (SharedData.standing[SharedData.currentSession][i].lapDiff > 0) // lapped
                                    {
                                        driverDiffLabel.Content = theme.translation["behind"] + SharedData.standing[SharedData.currentSession][i].lapDiff + theme.translation["lap"];
                                    }
                                    else // not lapped
                                    {
                                        if (SharedData.standing[SharedData.currentSession][i].diff > 0)
                                        { // in same lap
                                            if (SharedData.sidepanelType == SharedData.sidepanelTypes.fastlap)
                                                driverDiffLabel.Content = floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap, true, false);
                                            else
                                                driverDiffLabel.Content = theme.translation["behind"] + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff, true, false);
                                        }
                                        else // leader
                                            driverDiffLabel.Content = theme.translation["leader"];
                                    }
                                }
                                // prac/qual
                                else
                                {
                                    if (i == 0)
                                        driverDiffLabel.Content = floatTime2String(SharedData.standing[SharedData.currentSession][0].fastLap, true, false);
                                    else if (SharedData.standing[SharedData.currentSession][i].diff > 0)
                                    {
                                        if (SharedData.sidepanelType == SharedData.sidepanelTypes.fastlap)
                                            driverDiffLabel.Content = floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap, true, false);
                                        else
                                            driverDiffLabel.Content = theme.translation["behind"] + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff - SharedData.standing[SharedData.currentSession][0].diff, true, false);
                                    }
                                    else
                                        driverDiffLabel.Content = "-.--";
                                }
                            }
                        }
                        if (noLapsDriver)
                        {
                            driverPosLabel.Content = null;
                            driverDiffLabel.Content = "-.--";
                        }
                    }
                }

                // oSidepanel
                if (SharedData.standing[SharedData.currentSession] != null && SharedData.visible[(int)SharedData.overlayObjects.sidepanel])
                {
                    int sidepanelCount = 0;
                    for (int i = 0; i < SharedData.standing[SharedData.currentSession].Length; i++)
                    {
                        // diff to followed
                        if (SharedData.standing[SharedData.currentSession][i].id == SharedData.sessions[SharedData.currentSession].driverFollowed && SharedData.sidepanelType == SharedData.sidepanelTypes.followed)
                        {
                            int k = i - (theme.sidepanel.size / 2);
                            while (k < 0)
                                k++;
                            while ((k + theme.sidepanel.size) > SharedData.standing[SharedData.currentSession].Length && k > 0)
                            {
                                if (k > 0)
                                    k--;
                            }

                            for (int j = 0; j < theme.sidepanel.size; j++)
                            {
                                if (k < SharedData.standing[SharedData.currentSession].Length)
                                {
                                    sidepanelPosLabel[j].Content = theme.formatText(theme.sidepanel.Num.text, SharedData.standing[SharedData.currentSession][k].id, SharedData.currentSession); //String.Format(theme.sidepanel.Num.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id], k));
                                    sidepanelNameLabel[j].Content = theme.formatText(theme.sidepanel.Name.text, SharedData.standing[SharedData.currentSession][k].id, SharedData.currentSession); //String.Format(theme.sidepanel.Name.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id]));
                                    sidepanelInfoLabel[j].Content = theme.formatText(theme.sidepanel.Info.text, SharedData.standing[SharedData.currentSession][k].id, SharedData.currentSession); //String.Format(theme.sidepanel.Info.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id]));

                                    if (i != k)
                                    {
                                        if (k < i)
                                        {
                                            if (SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace) // race
                                            {
                                                if (SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id].onTrack == false && allowRetire) // out
                                                    sidepanelDiffLabel[j].Content = theme.translation["out"];
                                                else if (SharedData.standing[SharedData.currentSession][k].lapDiff == SharedData.standing[SharedData.currentSession][i].lapDiff) // same lap
                                                    sidepanelDiffLabel[j].Content = theme.translation["ahead"] + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff - SharedData.standing[SharedData.currentSession][k].diff, true, false);
                                                else // lapped
                                                    sidepanelDiffLabel[j].Content = theme.translation["ahead"] + Math.Abs(SharedData.standing[SharedData.currentSession][k].lapDiff - SharedData.standing[SharedData.currentSession][i].lapDiff) + theme.translation["lap"];
                                            }
                                            else // prac / qual
                                                sidepanelDiffLabel[j].Content = theme.translation["ahead"] + floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap - SharedData.standing[SharedData.currentSession][k].fastLap, true, false);
                                        }
                                        else
                                        {
                                            if (SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace) // race
                                            {
                                                if (SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id].onTrack == false && allowRetire) // out
                                                    sidepanelDiffLabel[j].Content = theme.translation["out"];
                                                else if (SharedData.standing[SharedData.currentSession][k].lapDiff == SharedData.standing[SharedData.currentSession][i].lapDiff) // same lap
                                                    sidepanelDiffLabel[j].Content = theme.translation["behind"] + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff - SharedData.standing[SharedData.currentSession][k].diff, true, false);
                                                else // lapped
                                                    sidepanelDiffLabel[j].Content = theme.translation["behind"] + Math.Abs(SharedData.standing[SharedData.currentSession][i].lapDiff - SharedData.standing[SharedData.currentSession][k].lapDiff) + theme.translation["lap"];
                                            }
                                            else // prac / qual
                                                sidepanelDiffLabel[j].Content = theme.translation["behind"] + floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap - SharedData.standing[SharedData.currentSession][k].fastLap, true, false);
                                        }

                                    }
                                    else
                                    {
                                        sidepanelDiffLabel[j].Content = "-.--";
                                    }
                                    k++;
                                    sidepanelCount++;
                                }
                            }
                        }
                        // diff to leader
                        if (SharedData.sidepanelType == SharedData.sidepanelTypes.leader && i < theme.sidepanel.size)
                        {
                            sidepanelPosLabel[i].Content = theme.formatText(theme.sidepanel.Num.text, SharedData.standing[SharedData.currentSession][i].id, SharedData.currentSession); //String.Format(theme.sidepanel.Num.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id], i));
                            sidepanelNameLabel[i].Content = theme.formatText(theme.sidepanel.Name.text, SharedData.standing[SharedData.currentSession][i].id, SharedData.currentSession); //String.Format(theme.sidepanel.Name.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));
                            sidepanelInfoLabel[i].Content = theme.formatText(theme.sidepanel.Info.text, SharedData.standing[SharedData.currentSession][i].id, SharedData.currentSession); //String.Format(theme.sidepanel.Info.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));

                            if (i > 0)
                            {
                                if (SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace)
                                {
                                    if (SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id].onTrack == false && allowRetire) // out
                                        sidepanelDiffLabel[i].Content = theme.translation["out"];
                                    else if (SharedData.standing[SharedData.currentSession][i].lapDiff > 0)
                                        sidepanelDiffLabel[i].Content = theme.translation["behind"] + SharedData.standing[SharedData.currentSession][i].lapDiff + theme.translation["lap"]; // lapped
                                    else
                                        sidepanelDiffLabel[i].Content = theme.translation["behind"] + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff, true, false); // normal
                                }
                                else // prac/qual
                                    sidepanelDiffLabel[i].Content = theme.translation["behind"] + floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap - SharedData.standing[SharedData.currentSession][0].fastLap, true, false);
                            }
                            else // leader
                            {
                                if (SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace)
                                {
                                    if (SharedData.drivers[SharedData.standing[SharedData.currentSession][0].id].onTrack == false && allowRetire) // out
                                        sidepanelDiffLabel[0].Content = theme.translation["out"];
                                    else // normal
                                        sidepanelDiffLabel[0].Content = null;
                                }
                                else // prac/qual
                                    sidepanelDiffLabel[0].Content = floatTime2String(SharedData.standing[SharedData.currentSession][0].fastLap, true, true);

                            }
                            sidepanelCount++;
                        }
                        // fastest lap
                        if (SharedData.sidepanelType == SharedData.sidepanelTypes.fastlap && i < theme.sidepanel.size)
                        {
                            sidepanelPosLabel[i].Content = theme.formatText(theme.sidepanel.Num.text, SharedData.standing[SharedData.currentSession][i].id, SharedData.currentSession); //String.Format(theme.sidepanel.Num.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id], i));
                            sidepanelNameLabel[i].Content = theme.formatText(theme.sidepanel.Name.text, SharedData.standing[SharedData.currentSession][i].id, SharedData.currentSession);//String.Format(theme.sidepanel.Name.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));
                            sidepanelInfoLabel[i].Content = theme.formatText(theme.sidepanel.Info.text, SharedData.standing[SharedData.currentSession][i].id, SharedData.currentSession);//String.Format(theme.sidepanel.Info.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));
                            sidepanelDiffLabel[i].Content = floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap, true, false);
                            sidepanelCount++;
                        }
                    }
                }

                // results update
                if (SharedData.resultSession >= 0 && SharedData.standing[SharedData.resultSession] != null)
                {
                    // header
                    if (SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypeRace)
                        resultsHeader.Content = String.Format(theme.resultsHeader.text, theme.translation["race"]);
                    else if (SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypeQualifyLone ||
                             SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypeQualifyOpen)
                        resultsHeader.Content = String.Format(theme.resultsHeader.text, theme.translation["qualify"]);
                    else if (SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypePractice ||
                             SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypePracticeLone ||
                             SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypeTesting)
                        resultsHeader.Content = String.Format(theme.resultsHeader.text, theme.translation["practice"]);

                    if (SharedData.sessions[SharedData.resultSession].laps == iRacingTelem.LAPS_UNLIMITED)
                        resultsSubHeader.Content = String.Format(theme.resultsSubHeader.text, Math.Floor((SharedData.sessions[SharedData.resultSession].time - SharedData.sessions[SharedData.resultSession].timeRemaining) / 60), theme.translation["minutes"]);
                    else
                        resultsSubHeader.Content = String.Format(theme.resultsSubHeader.text, SharedData.sessions[SharedData.resultSession].laps - SharedData.sessions[SharedData.resultSession].lapsRemaining, theme.translation["laps"]);

                    for (int i = theme.results.size * SharedData.resultPage; i <= ((theme.results.size * (SharedData.resultPage + 1)) - 1); i++)
                    {
                        int j;
                        if (SharedData.resultPage > 0)
                            j = i % (theme.results.size * SharedData.resultPage);
                        else
                            j = i;

                        if (i < SharedData.standing[SharedData.currentSession].Length)
                        {
                            resultsPosLabel[j].Content = theme.formatText(theme.results.Num.text, SharedData.standing[SharedData.resultSession][i].id, SharedData.resultSession); //String.Format(theme.results.Num.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.resultSession][i].id], i));
                            resultsNameLabel[j].Content = theme.formatText(theme.results.Name.text, SharedData.standing[SharedData.resultSession][i].id, SharedData.resultSession); //String.Format(theme.results.Name.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.resultSession][i].id]));
                            resultsInfoLabel[j].Content = theme.formatText(theme.results.Info.text, SharedData.standing[SharedData.resultSession][i].id, SharedData.resultSession); //String.Format(theme.results.Info.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.resultSession][i].id]));


                            if (SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypeRace)
                            {
                                if (SharedData.drivers[SharedData.standing[SharedData.resultSession][i].id].onTrack == false && allowRetire) // out
                                    resultsDiffLabel[j].Content = theme.translation["out"];
                                else if (i == 0)
                                    if (SharedData.sidepanelType == SharedData.sidepanelTypes.fastlap)
                                        resultsDiffLabel[j].Content = floatTime2String(SharedData.standing[SharedData.resultSession][0].fastLap, true, false);
                                    else
                                        resultsDiffLabel[j].Content = Math.Floor(SharedData.standing[SharedData.resultSession][0].completedLaps) + " " + theme.translation["laps"];
                                else if (SharedData.standing[SharedData.resultSession][i].lapDiff > 0) // lapped
                                    resultsDiffLabel[j].Content = theme.translation["behind"] + SharedData.standing[SharedData.resultSession][i].lapDiff + theme.translation["lap"];
                                else
                                { // normal
                                    if (SharedData.sidepanelType == SharedData.sidepanelTypes.fastlap)
                                        resultsDiffLabel[j].Content = floatTime2String(SharedData.standing[SharedData.resultSession][i].fastLap, true, false);
                                    else
                                        resultsDiffLabel[j].Content = theme.translation["behind"] + floatTime2String(SharedData.standing[SharedData.resultSession][i].diff - SharedData.standing[SharedData.resultSession][0].diff, true, false);
                                }
                            }
                            else if (i == 0)
                                resultsDiffLabel[j].Content = floatTime2String(SharedData.standing[SharedData.resultSession][0].fastLap, true, false);
                            else
                            {
                                if (SharedData.sidepanelType == SharedData.sidepanelTypes.fastlap)
                                    resultsDiffLabel[j].Content = floatTime2String(SharedData.standing[SharedData.resultSession][i].fastLap, true, false);
                                else
                                    resultsDiffLabel[j].Content = theme.translation["behind"] + floatTime2String(SharedData.standing[SharedData.resultSession][i].fastLap - SharedData.standing[SharedData.resultSession][0].fastLap, true, false);
                            }

                            if (resultsPosLabel[j].Visibility == System.Windows.Visibility.Hidden)
                            {
                                resultsPosLabel[j].Visibility = System.Windows.Visibility.Visible;
                                resultsNameLabel[j].Visibility = System.Windows.Visibility.Visible;
                                resultsDiffLabel[j].Visibility = System.Windows.Visibility.Visible;
                                resultsInfoLabel[j].Visibility = System.Windows.Visibility.Visible;
                            }

                        }
                        else
                        {
                            resultsPosLabel[j].Visibility = System.Windows.Visibility.Hidden;
                            resultsNameLabel[j].Visibility = System.Windows.Visibility.Hidden;
                            resultsDiffLabel[j].Visibility = System.Windows.Visibility.Hidden;
                            resultsInfoLabel[j].Visibility = System.Windows.Visibility.Hidden;
                        }

                        if (i == (SharedData.standing[SharedData.currentSession].Length - 1))
                            SharedData.resultLastPage = true;
                    }
                }

                // session state
                if (SharedData.visible[(int)SharedData.overlayObjects.sessionstate])
                {
                    if (SharedData.sessions[SharedData.currentSession].laps == iRacingTelem.LAPS_UNLIMITED)
                    {
                        if(SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateCheckered) // session ending
                            sessionstateText.Content = theme.translation["finishing"];
                        else // normal
                            sessionstateText.Content = floatTime2String(SharedData.sessions[SharedData.currentSession].timeRemaining, false, true);
                    }
                    else if (SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateGetInCar)
                    {
                        sessionstateText.Content = theme.translation["gridding"];
                    }
                    else if (SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateParadeLaps)
                    {
                        sessionstateText.Content = theme.translation["pacelap"];
                    }
                    else
                    {
                        int currentlap = (SharedData.sessions[SharedData.currentSession].laps - SharedData.sessions[SharedData.currentSession].lapsRemaining);
                        if (SharedData.sessions[SharedData.currentSession].lapsRemaining < 1)
                        {
                            sessionstateText.Content = theme.translation["finishing"];
                        }
                        else if (SharedData.sessions[SharedData.currentSession].lapsRemaining == 1)
                        {
                            sessionstateText.Content = theme.translation["finallap"];
                        }
                        else if (SharedData.sessions[SharedData.currentSession].lapsRemaining <= Properties.Settings.Default.countdownThreshold) // x laps remaining
                            sessionstateText.Content = String.Format("{0} {1} {2}",
                                SharedData.sessions[SharedData.currentSession].lapsRemaining,
                                theme.translation["laps"],
                                theme.translation["remaining"]
                            ); 
                        else // normal behavior
                        {
                            sessionstateText.Content = String.Format("{0} {1} {2} {3}",
                                theme.translation["lap"],
                                currentlap,
                                theme.translation["of"],
                                SharedData.sessions[SharedData.currentSession].laps
                            );

                        }
                    }
                }

                // start lights
                if (SharedData.visible[(int)SharedData.overlayObjects.startlights])
                {
                    if (SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateWarmup ||
                        SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateRacing)
                    {
                        timer = (DateTime.Now - SharedData.startlights);

                        if ((SharedData.sessions[SharedData.currentSession].laps - SharedData.sessions[SharedData.currentSession].lapsRemaining) < 0)
                        {
                            if (timer.TotalMinutes > 1) // reset
                                SharedData.startlights = DateTime.Now;
                        }

                        if (timer.TotalSeconds < 5 || timer.TotalMinutes > 1)
                        {
                            themeImages[(int)Theme.overlayTypes.lightsoff].Visibility = System.Windows.Visibility.Visible;
                            themeImages[(int)Theme.overlayTypes.lightsred].Visibility = System.Windows.Visibility.Hidden;
                            themeImages[(int)Theme.overlayTypes.lightsgreen].Visibility = System.Windows.Visibility.Hidden;
                        }
                        else if (SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateRacing)
                        {
                            themeImages[(int)Theme.overlayTypes.lightsoff].Visibility = System.Windows.Visibility.Hidden;
                            themeImages[(int)Theme.overlayTypes.lightsred].Visibility = System.Windows.Visibility.Hidden;
                            themeImages[(int)Theme.overlayTypes.lightsgreen].Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            themeImages[(int)Theme.overlayTypes.lightsoff].Visibility = System.Windows.Visibility.Hidden;
                            themeImages[(int)Theme.overlayTypes.lightsred].Visibility = System.Windows.Visibility.Visible;
                            themeImages[(int)Theme.overlayTypes.lightsgreen].Visibility = System.Windows.Visibility.Hidden;
                        }
                    }
                }

                // flags
                foreach (int flag in flags)
                    if (themeImages[flag] != null)
                        themeImages[flag].Visibility = System.Windows.Visibility.Hidden; // reset

                if (SharedData.visible[(int)SharedData.overlayObjects.flag])
                {
                    if (SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateRacing)
                    {
                        if (SharedData.sessions[SharedData.currentSession].flag == iRacingTelem.eSessionFlag.kFlagYellow)
                            themeImages[(int)Theme.overlayTypes.flagyellow].Visibility = System.Windows.Visibility.Visible;
                        else if (SharedData.sessions[SharedData.currentSession].lapsRemaining == 1)
                            themeImages[(int)Theme.overlayTypes.flagwhite].Visibility = System.Windows.Visibility.Visible;
                        else if (SharedData.sessions[SharedData.currentSession].lapsRemaining <= 0)
                            themeImages[(int)Theme.overlayTypes.flagcheckered].Visibility = System.Windows.Visibility.Visible;
                        else
                            themeImages[(int)Theme.overlayTypes.flaggreen].Visibility = System.Windows.Visibility.Visible;
                    }
                    else if (SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateCheckered ||
                        SharedData.sessions[SharedData.currentSession].state == iRacingTelem.eSessionState.kSessionStateCoolDown)
                        themeImages[(int)Theme.overlayTypes.flagcheckered].Visibility = System.Windows.Visibility.Visible;
                }

                if (SharedData.visible[(int)SharedData.overlayObjects.ticker] && SharedData.standing[SharedData.currentSession].Length > 0)
                {
                    Thickness scroller;

                    if (ticker.Margin.Left + ticker.ActualWidth <= 0 ||
                        ticker.Margin.Left > theme.ticker.width) // ticker is hidden
                    {
                        int itemcount = SharedData.standing[SharedData.currentSession].Length;
                        if (itemcount != (ticker.Children.Count / 3))
                        {
                            ticker.Children.Clear();

                            tickerPosLabel = new Label[itemcount];
                            tickerNameLabel = new Label[itemcount];
                            tickerDiffLabel = new Label[itemcount];
                            tickerInfoLabel = new Label[itemcount];

                            for (int i = 0; i < itemcount; i++)
                            {
                                tickerPosLabel[i] = DrawLabel(theme.ticker.Num);
                                tickerNameLabel[i] = DrawLabel(theme.ticker.Name);
                                tickerDiffLabel[i] = DrawLabel(theme.ticker.Diff);
                                tickerInfoLabel[i] = DrawLabel(theme.ticker.Info);

                                tickerPosLabel[i].Width = Double.NaN;
                                tickerNameLabel[i].Width = Double.NaN;
                                tickerDiffLabel[i].Width = Double.NaN;
                                tickerInfoLabel[i].Width = Double.NaN;

                                if(theme.ticker.Num.text != "")
                                    ticker.Children.Add(tickerPosLabel[i]);
                                if (theme.ticker.Name.text != "")
                                    ticker.Children.Add(tickerNameLabel[i]);
                                if (theme.ticker.Diff.text != "")
                                    ticker.Children.Add(tickerDiffLabel[i]);
                                if (theme.ticker.Info.text != "")
                                    ticker.Children.Add(tickerInfoLabel[i]);
                                
                            }

                        }

                        // move ticker for new scroll
                        scroller = ticker.Margin;
                        scroller.Left = theme.ticker.width;
                        ticker.Margin = scroller;
                    }
                    else // ticker visible
                    {
                        for (int i = 0; i < tickerPosLabel.Length; i++) // update data
                        {

                            tickerPosLabel[i].Content = theme.formatText(theme.ticker.Num.text, SharedData.standing[SharedData.currentSession][i].id, SharedData.currentSession); //String.Format(theme.ticker.Num.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id], i));
                            tickerNameLabel[i].Content = theme.formatText(theme.ticker.Name.text, SharedData.standing[SharedData.currentSession][i].id, SharedData.currentSession); //String.Format(theme.ticker.Name.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));
                            tickerInfoLabel[i].Content = theme.formatText(theme.ticker.Info.text, SharedData.standing[SharedData.currentSession][i].id, SharedData.currentSession); //String.Format(theme.ticker.Info.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));


                            if (SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace)
                            {
                                if (SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id].onTrack == false && allowRetire) // out
                                    tickerDiffLabel[i].Content = theme.translation["out"];
                                else if (i == 0)
                                    if (SharedData.sidepanelType == SharedData.sidepanelTypes.fastlap)
                                        tickerDiffLabel[i].Content = floatTime2String(SharedData.standing[SharedData.currentSession][0].fastLap, true, false);
                                    else
                                        tickerDiffLabel[i].Content = Math.Floor(SharedData.standing[SharedData.currentSession][0].completedLaps) + " " + theme.translation["laps"];
                                else if (SharedData.standing[SharedData.currentSession][i].lapDiff > 0) // lapped
                                    tickerDiffLabel[i].Content = theme.translation["behind"] + SharedData.standing[SharedData.currentSession][i].lapDiff + theme.translation["lap"];
                                else
                                { // normal
                                    if (SharedData.sidepanelType == SharedData.sidepanelTypes.fastlap)
                                        tickerDiffLabel[i].Content = floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap, true, false);
                                    else
                                        tickerDiffLabel[i].Content = theme.translation["behind"] + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff - SharedData.standing[SharedData.currentSession][0].diff, true, false);
                                }
                            }
                            else if (i == 0)
                                tickerDiffLabel[i].Content = floatTime2String(SharedData.standing[SharedData.currentSession][0].fastLap, true, false);
                            else
                            {
                                if (SharedData.sidepanelType == SharedData.sidepanelTypes.fastlap)
                                    tickerDiffLabel[i].Content = floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap, true, false);
                                else
                                    tickerDiffLabel[i].Content = theme.translation["behind"] + floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap - SharedData.standing[SharedData.currentSession][0].fastLap, true, false);
                            }
                        }

                        // scroll
                        scroller = ticker.Margin;
                        scroller.Left -= Properties.Settings.Default.TickerSpeed;
                        ticker.Margin = scroller;
                    }
                }

                // laptime
                if (SharedData.visible[(int)SharedData.overlayObjects.laptime])
                {
                    //laptimeText.Content = String.Format(theme.laptimeText.text, theme.getFormats(SharedData.drivers[SharedData.sessions[SharedData.currentSession].driverFollowed]));
                    laptimeText.Content = theme.formatText(theme.laptimeText.text, SharedData.sessions[SharedData.currentSession].driverFollowed, SharedData.currentSession);
                }
                 */
                stopwatch.Stop();
                SharedData.overlayEffectiveFPSstack.Push((float)stopwatch.Elapsed.TotalMilliseconds);
            }

            

        }

        public static string floatTime2String(float time, Boolean showMilli, Boolean showMinutes)
        {
            time = Math.Abs(time);

            int hours = (int)Math.Floor(time / 3600);
            int minutes = (int)Math.Floor((time - (hours * 3600)) / 60);
            int seconds = (int)Math.Floor(time % 60);
            int microseconds = (int)Math.Round(time * 1000 % 1000, 3);
            string output;

            if (time == 0.0)
                output = "-.--";
            else if (hours > 0)
            {
                output = String.Format("{0}:{1:d2}:{2:d2}", hours, minutes, seconds);
            }
            else if (minutes > 0 || showMinutes)
            {
                if (showMilli)
                    output = String.Format("{0}:{1:d2}.{2:d3}", minutes, seconds, microseconds);
                else
                    output = String.Format("{0}:{1:d2}", minutes, seconds);
            }

            else
            {
                if (showMilli)
                    output = String.Format("{0}.{1:d3}", seconds, microseconds);
                else
                    output = String.Format("{0}", seconds);
            }

            return output;
        }
        
    }
}