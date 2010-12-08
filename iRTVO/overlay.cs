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

namespace iRTVO
{
    public partial class Overlay : Window
    {

        Canvas driver;
        Label driverPosLabel;
        Label driverNameLabel;
        Label driverDiffLabel;

        Canvas sidepanel;
        Label[] sidepanelPosLabel;
        Label[] sidepanelNameLabel; 
        Label[] sidepanelDiffLabel;

        Canvas results;
        Label resultsHeader;
        Label resultsSubHeader;
        Label[] resultsPosLabel;
        Label[] resultsNameLabel;
        Label[] resultsDiffLabel;

        private void overlayUpdate(object sender, EventArgs e)
        {

            if (SharedData.requestRefresh == true)
            {
                loadTheme(theme.name);
                resizeOverlay(overlay.Width, overlay.Height);
                SharedData.requestRefresh = false;
            }

            // wait
            SharedData.driversMutex.WaitOne(5);
            SharedData.standingMutex.WaitOne(5);
            SharedData.sessionsMutex.WaitOne(5);

            /* disabled temporarily
            // hide/show objects
            // driver
            if (SharedData.visible[(int)SharedData.overlayObjects.driver])
                 driver.Visibility = System.Windows.Visibility.Visible;
            else
                 driver.Visibility = System.Windows.Visibility.Hidden;

            // sidepanel
            if (SharedData.visible[(int)SharedData.overlayObjects.sidepanel])
                sidepanel.Visibility = System.Windows.Visibility.Visible;
            else
                sidepanel.Visibility = System.Windows.Visibility.Hidden;

            // replay
            if (SharedData.visible[(int)SharedData.overlayObjects.replay])
                oReplay.Visibility = System.Windows.Visibility.Visible;
            else
                oReplay.Visibility = System.Windows.Visibility.Hidden;

            // replay
            if (SharedData.visible[(int)SharedData.overlayObjects.sessionstatus])
                oSessionState.Visibility = System.Windows.Visibility.Visible;
            else
                oSessionState.Visibility = System.Windows.Visibility.Hidden;
            */

            // hide/show objects
            // driver
            if (SharedData.visible[(int)SharedData.overlayObjects.driver])
            {
                if (themeImages[(int)overlayTypes.driver] != null)
                    themeImages[(int)overlayTypes.driver].Visibility = System.Windows.Visibility.Visible;
                driver.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                if (themeImages[(int)overlayTypes.driver] != null)
                    themeImages[(int)overlayTypes.driver].Visibility = System.Windows.Visibility.Hidden;
                driver.Visibility = System.Windows.Visibility.Hidden;

            }

            // sidepanel
            if (SharedData.visible[(int)SharedData.overlayObjects.sidepanel])
            {
                if (themeImages[(int)overlayTypes.sidepanel] != null)
                    themeImages[(int)overlayTypes.sidepanel].Visibility = System.Windows.Visibility.Visible;
                sidepanel.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                if (themeImages[(int)overlayTypes.sidepanel] != null)
                themeImages[(int)overlayTypes.sidepanel].Visibility = System.Windows.Visibility.Hidden;
                sidepanel.Visibility = System.Windows.Visibility.Hidden;
            }

            // replay
            if (SharedData.visible[(int)SharedData.overlayObjects.replay])
                themeImages[(int)overlayTypes.replay].Visibility = System.Windows.Visibility.Visible;
            else
                themeImages[(int)overlayTypes.replay].Visibility = System.Windows.Visibility.Hidden;

            // replay
            if (SharedData.visible[(int)SharedData.overlayObjects.sessionstatus])
                themeImages[(int)overlayTypes.sessionstate].Visibility = System.Windows.Visibility.Visible;
            else
                themeImages[(int)overlayTypes.sessionstate].Visibility = System.Windows.Visibility.Hidden;

            // results
            if (SharedData.visible[(int)SharedData.overlayObjects.results])
            {
                if (themeImages[(int)overlayTypes.replay] != null)
                    themeImages[(int)overlayTypes.results].Visibility = System.Windows.Visibility.Visible;
                results.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                if (themeImages[(int)overlayTypes.results] != null)
                    themeImages[(int)overlayTypes.results].Visibility = System.Windows.Visibility.Hidden;
                results.Visibility = System.Windows.Visibility.Hidden;
            }

            //  driver
            if (SharedData.visible[(int)SharedData.overlayObjects.driver])
            {
                Boolean noLapsDriver = true;
                driverNameLabel.Content = SharedData.drivers[SharedData.sessions[SharedData.currentSession].driverFollowed].name;
                if (SharedData.standing[SharedData.currentSession] != null)
                {
                    for (int i = 0; i < SharedData.standing[SharedData.currentSession].Length; i++)
                    {
                        // update  driver
                        if (SharedData.standing[SharedData.currentSession][i].id == SharedData.sessions[SharedData.currentSession].driverFollowed)
                        {
                            noLapsDriver = false;
                            driverPosLabel.Content = (i + 1).ToString() + ".";

                            // race
                            if (SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace)
                            {
                                if (SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id].onTrack == false) // out
                                    driverDiffLabel.Content = i18n.out_short;
                                else if (SharedData.standing[SharedData.currentSession][i].lapDiff > 0) // lapped
                                {
                                    driverDiffLabel.Content = "+" + SharedData.standing[SharedData.currentSession][i].lapDiff + i18n.lap_short;
                                }
                                else // not lapped
                                {
                                    if (SharedData.standing[SharedData.currentSession][i].diff > 0) // in same lap
                                        driverDiffLabel.Content = "+" + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff, true, false);
                                    else // leader
                                        driverDiffLabel.Content = "-.--";
                                }
                            }
                            // prac/qual
                            else
                            {
                                if (i == 0)
                                    driverDiffLabel.Content = floatTime2String(SharedData.standing[SharedData.currentSession][0].fastLap, true, false);
                                else if (SharedData.standing[SharedData.currentSession][i].diff > 0)
                                    driverDiffLabel.Content = "+" + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff - SharedData.standing[SharedData.currentSession][0].diff, true, false);
                                else
                                    driverDiffLabel.Content = "-.--";
                            }
                        }
                    }
                    if (noLapsDriver)
                    {
                        driverPosLabel.Content = SharedData.standing[SharedData.currentSession].Length + ".";
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
                        int k = i - (theme.sidepanel.size/2);
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
                                sidepanelPosLabel[j].Content = (k + 1).ToString();
                                sidepanelNameLabel[j].Content = SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id].initials;

                                if (i != k)
                                {
                                    if (k < i)
                                    {
                                        if (SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace) // race
                                        {
                                            if (SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id].onTrack == false && 
                                                SharedData.sessions[SharedData.currentSession].state != iRacingTelem.eSessionState.kSessionStateCoolDown) // out
                                                sidepanelDiffLabel[j].Content = i18n.out_short;
                                            else if (SharedData.standing[SharedData.currentSession][k].lapDiff == SharedData.standing[SharedData.currentSession][i].lapDiff) // same lap
                                                sidepanelDiffLabel[j].Content = "-" + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff - SharedData.standing[SharedData.currentSession][k].diff, true, false);
                                            else // lapped
                                                sidepanelDiffLabel[j].Content = "-" + Math.Abs(SharedData.standing[SharedData.currentSession][k].lapDiff - SharedData.standing[SharedData.currentSession][i].lapDiff) + i18n.lap_short;
                                        }
                                        else // prac / qual
                                            sidepanelDiffLabel[j].Content = "-" + floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap - SharedData.standing[SharedData.currentSession][k].fastLap, true, false);
                                    }
                                    else
                                    {
                                        if (SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace) // race
                                        {
                                            if (SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id].onTrack == false &&
                                                SharedData.sessions[SharedData.currentSession].state != iRacingTelem.eSessionState.kSessionStateCoolDown) // out
                                                sidepanelDiffLabel[j].Content = i18n.out_short;
                                            else if (SharedData.standing[SharedData.currentSession][k].lapDiff == SharedData.standing[SharedData.currentSession][i].lapDiff) // same lap
                                                sidepanelDiffLabel[j].Content = "+" + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff - SharedData.standing[SharedData.currentSession][k].diff, true, false);
                                            else // lapped
                                                sidepanelDiffLabel[j].Content = "+" + Math.Abs(SharedData.standing[SharedData.currentSession][i].lapDiff - SharedData.standing[SharedData.currentSession][k].lapDiff) + i18n.lap_short;
                                        }
                                        else // prac / qual
                                            sidepanelDiffLabel[j].Content = "+" + floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap - SharedData.standing[SharedData.currentSession][k].fastLap, true, false);
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
                        sidepanelPosLabel[i].Content = (i + 1).ToString();
                        sidepanelNameLabel[i].Content = SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id].initials;
                        if (i > 0)
                        {
                            if(SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace) 
                            {
                                if (SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id].onTrack == false &&
                                    SharedData.sessions[SharedData.currentSession].state != iRacingTelem.eSessionState.kSessionStateCoolDown)
                                    sidepanelDiffLabel[i].Content = i18n.out_short;
                                else if (SharedData.standing[SharedData.currentSession][i].lapDiff > 0)
                                    sidepanelDiffLabel[i].Content = "+" + SharedData.standing[SharedData.currentSession][i].lapDiff + i18n.lap_short;
                                else
                                    sidepanelDiffLabel[i].Content = "+" + floatTime2String(SharedData.standing[SharedData.currentSession][i].diff, true, false);
                            }
                            else
                                sidepanelDiffLabel[i].Content = "+" + floatTime2String(SharedData.standing[SharedData.currentSession][i].fastLap - SharedData.standing[SharedData.currentSession][0].fastLap, true, false);
                        }
                        else
                        {
                            if (SharedData.sessions[SharedData.currentSession].type == iRacingTelem.eSessionType.kSessionTypeRace)
                            {
                                if(SharedData.drivers[SharedData.standing[SharedData.currentSession][0].id].onTrack == false &&
                                    SharedData.sessions[SharedData.currentSession].state != iRacingTelem.eSessionState.kSessionStateCoolDown)
                                    sidepanelDiffLabel[0].Content = i18n.out_short;
                                else
                                    sidepanelDiffLabel[0].Content = floatTime2String((SharedData.sessions[SharedData.currentSession].time - SharedData.sessions[SharedData.currentSession].timeRemaining), false, true);
                            }
                            else
                                sidepanelDiffLabel[0].Content = floatTime2String(SharedData.standing[SharedData.currentSession][0].fastLap, true, true);

                        }
                        sidepanelCount++;
                    }
                        
                }
                /* hidden
                if (sidepanelCount < theme.sidepanel.size)
                {
                    for (int i = sidepanelCount; i < 10; i++)
                    {
                        sidepanelPosLabel[i].Visibility = System.Windows.Visibility.Hidden;
                        sidepanelNameLabel[i].Visibility = System.Windows.Visibility.Hidden;
                        sidepanelDiffLabel[i].Visibility = System.Windows.Visibility.Hidden;
                        sidepanelPosRect[i].Visibility = System.Windows.Visibility.Hidden;
                        sidepanelNameRect[i].Visibility = System.Windows.Visibility.Hidden;
                        sidepanelDiffRect[i].Visibility = System.Windows.Visibility.Hidden;
                    }
                }
                */
            }

            // results update
            if (SharedData.resultSession >= 0 && SharedData.standing[SharedData.resultSession] != null)
            {
                // header

                if (SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypeRace)
                    resultsHeader.Content = i18n.race_results;
                else if (SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypeQualifyLone ||
                         SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypeQualifyOpen)
                    resultsHeader.Content = i18n.qualify_results;
                else if (SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypePractice ||
                         SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypePracticeLone ||
                         SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypeTesting)
                    resultsHeader.Content = i18n.practice_results;
                
                for (int i = theme.results.size * SharedData.resultPage; i <= ((theme.results.size * (SharedData.resultPage + 1)) - 1); i++)
                {
                    int j;
                    if (SharedData.resultPage > 0)
                        j = i % (theme.results.size * SharedData.resultPage);
                    else
                        j = i;

                    if (i < SharedData.standing[SharedData.currentSession].Length)
                    {
                        resultsPosLabel[j].Content = (i + 1).ToString();
                        resultsNameLabel[j].Content = SharedData.drivers[SharedData.standing[SharedData.resultSession][i].id].name;

                        if (SharedData.sessions[SharedData.resultSession].type == iRacingTelem.eSessionType.kSessionTypeRace)
                        {
                            if (SharedData.drivers[SharedData.standing[SharedData.resultSession][i].id].onTrack == false &&
                                SharedData.sessions[SharedData.resultSession].state != iRacingTelem.eSessionState.kSessionStateCoolDown)
                                resultsDiffLabel[j].Content = i18n.out_short;
                            else if (i == 0)
                                resultsDiffLabel[j].Content = Math.Floor(SharedData.standing[SharedData.resultSession][0].completedLaps) + i18n.lap_short;
                            else if (SharedData.standing[SharedData.resultSession][i].lapDiff > 0)
                                resultsDiffLabel[j].Content = "+" + SharedData.standing[SharedData.resultSession][i].lapDiff + i18n.lap_short;
                            else
                                resultsDiffLabel[j].Content = "+" + floatTime2String(SharedData.standing[SharedData.resultSession][i].diff - SharedData.standing[SharedData.resultSession][0].diff, true, false);
                        }
                        else if (i == 0)
                            resultsDiffLabel[j].Content = floatTime2String(SharedData.standing[SharedData.resultSession][0].fastLap, true, true);
                        else
                        {
                            resultsDiffLabel[j].Content = "+" + floatTime2String(SharedData.standing[SharedData.resultSession][i].fastLap - SharedData.standing[SharedData.resultSession][0].fastLap, true, false);
                        }
                        
                        resultsPosLabel[j].Visibility = System.Windows.Visibility.Visible;
                        resultsNameLabel[j].Visibility = System.Windows.Visibility.Visible;
                        resultsDiffLabel[j].Visibility = System.Windows.Visibility.Visible;

                    }
                    else
                    {
                        resultsPosLabel[j].Visibility = System.Windows.Visibility.Hidden;
                        resultsNameLabel[j].Visibility = System.Windows.Visibility.Hidden;
                        resultsDiffLabel[j].Visibility = System.Windows.Visibility.Hidden;
                    }

                    if (i == (SharedData.standing[SharedData.currentSession].Length - 1))
                        SharedData.resultLastPage = true;
                }
            }
            /* disabled temporarily
            // oSessionRemaining 
            if (SharedData.sessions[SharedData.currentSession].laps == iRacingTelem.LAPS_UNLIMITED)
            {
                oSessionState.Content = floatTime2String(SharedData.sessions[SharedData.currentSession].timeRemaining, false, true);
            }
            else
            {
                oSessionState.Content = (SharedData.sessions[SharedData.currentSession].laps - SharedData.sessions[SharedData.currentSession].lapsRemaining) + "/" + SharedData.sessions[SharedData.currentSession].laps;
            }
            */
            //SharedData.driversMutexEvent.Reset();
            //SharedData.standingMutexEvent.Reset();
            //SharedData.sessionsMutexEvent.Reset();
        }

        private string floatTime2String(float time, Boolean showMilli, Boolean showMinutes)
        {
            time = Math.Abs(time);

            int hours = (int)Math.Floor(time / 3600);
            int minutes = (int)Math.Floor((time - (hours * 3600)) / 60);
            int seconds = (int)Math.Floor(time % 60);
            int microseconds = (int)Math.Round(time * 1000 % 1000, 3);
            string output;

            if (hours > 0)
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

        private void initOverlay()
        {
            /* disabled temporarily
            // init driver
             driverPosRect = DrawRectangle( driver, 75, 40, 0, 10, Properties.Settings.Default.color1, 5, 1);
             driverDiffRect = DrawRectangle( driver, 200, 40, 340, 10, Properties.Settings.Default.color3, 5, 1);
             driverNameRect = DrawRectangle( driver, 350, 40, 65, 10, Properties.Settings.Default.color2, 0, 1);

             driverPosLabel = DrawLabel( driver, 65, 60, 0, 0, Properties.Settings.Default.color2, "Calibri", 35, true, System.Windows.HorizontalAlignment.Center);
             driverNameLabel = DrawLabel( driver, 340, 60, 75, 0, Properties.Settings.Default.color3, "Calibri", 32, true, System.Windows.HorizontalAlignment.Left);
             driverDiffLabel = DrawLabel( driver, 120, 60, 415, 0, Properties.Settings.Default.color2, "Calibri", 24, false, System.Windows.HorizontalAlignment.Right);

             driverPosLabel.Content = "0.";
             driverNameLabel.Content = "-";
             driverDiffLabel.Content = "-.--";

             driver.Children.Add( driverPosRect);
             driver.Children.Add( driverDiffRect);
             driver.Children.Add( driverNameRect);
             driver.Children.Add( driverPosLabel);
             driver.Children.Add( driverDiffLabel);
             driver.Children.Add( driverNameLabel);

            // init sidepanel
            for (int i = 0; i < 10; i++)
            {
                sidepanelPosRect[i] = DrawRectangle(sidepanel, 28, 20, 0, i * 22, Properties.Settings.Default.color1, 2, 1);
                sidepanelDiffRect[i] = DrawRectangle(sidepanel, 65, 20, 55, i * 22, Properties.Settings.Default.color3, 2, 1);
                sidepanelNameRect[i] = DrawRectangle(sidepanel, 35, 20, 25, i * 22, Properties.Settings.Default.color2, 0, 1);

                sidepanelPosLabel[i] = DrawLabel(sidepanel, 28, 30, 0, (i * 22) - 5, Properties.Settings.Default.color2, "Calibri", 14, true, System.Windows.HorizontalAlignment.Center);
                sidepanelNameLabel[i] = DrawLabel(sidepanel, 35, 30, 25, (i * 22) - 5, Properties.Settings.Default.color3, "Consolas", 14, true, System.Windows.HorizontalAlignment.Left);
                sidepanelDiffLabel[i] = DrawLabel(sidepanel, 65, 30, 55, (i * 22) - 5, Properties.Settings.Default.color2, "Calibri", 11, false, System.Windows.HorizontalAlignment.Right);

                sidepanelPosLabel[i].Content = (i + 1).ToString();
                sidepanelNameLabel[i].Content = "---";
                sidepanelDiffLabel[i].Content = "-.--";

                sidepanel.Children.Add(sidepanelPosRect[i]);
                sidepanel.Children.Add(sidepanelDiffRect[i]);
                sidepanel.Children.Add(sidepanelNameRect[i]);
                sidepanel.Children.Add(sidepanelPosLabel[i]);
                sidepanel.Children.Add(sidepanelDiffLabel[i]);
                sidepanel.Children.Add(sidepanelNameLabel[i]);
            }
            sidepanel.Visibility = Visibility.Hidden;

            // init results
            for (int i = 0; i < 10; i++)
            {
                resultsPosRect[i] = DrawRectangle(results, 32, 23, 0, (i + 1) * 25, Properties.Settings.Default.color1, 2, 1);
                resultsDiffRect[i] = DrawRectangle(results, 62, 23, 238, (i + 1) * 25, Properties.Settings.Default.color3, 2, 1);
                resultsNameRect[i] = DrawRectangle(results, 210, 23, 30, (i + 1) * 25, Properties.Settings.Default.color2, 0, 1);

                resultsPosLabel[i] = DrawLabel(results, 33, 33, 0, ((i + 1) * 25) - 5, Properties.Settings.Default.color2, "Calibri", 16, true, System.Windows.HorizontalAlignment.Center);
                resultsNameLabel[i] = DrawLabel(results, 210, 33, 30, ((i + 1) * 25) - 5, Properties.Settings.Default.color3, "Calibri", 15, false, System.Windows.HorizontalAlignment.Left);
                resultsDiffLabel[i] = DrawLabel(results, 60, 33, 240, ((i + 1) * 25) - 5, Properties.Settings.Default.color2, "Calibri", 14, false, System.Windows.HorizontalAlignment.Right);

                resultsPosLabel[i].Content = (i + 1).ToString();
                resultsNameLabel[i].Content = "-";
                resultsDiffLabel[i].Content = "-.--";

                results.Children.Add(resultsPosRect[i]);
                results.Children.Add(resultsDiffRect[i]);
                results.Children.Add(resultsNameRect[i]);
                results.Children.Add(resultsPosLabel[i]);
                results.Children.Add(resultsDiffLabel[i]);
                results.Children.Add(resultsNameLabel[i]);
            }
            results.Visibility = Visibility.Hidden;
            */
        }
        /*
        private Rectangle DrawRectangle(Canvas canvas, int width, int height, int left, int top, System.Windows.Media.Color color, int curvature, int zIndex)
        {

            Rectangle rectangle = new Rectangle();
            SolidColorBrush myBrush = new SolidColorBrush(color);

            rectangle.Width = width;
            rectangle.Height = height;
            rectangle.Fill = myBrush;
            rectangle.RadiusX = curvature;
            rectangle.RadiusY = curvature;
            rectangle.SnapsToDevicePixels = true;

            Canvas.SetTop(rectangle, top);
            Canvas.SetLeft(rectangle, left);
            Canvas.SetZIndex(rectangle, zIndex);

            return rectangle;
        }
        */
        //private Label DrawLabel(Canvas canvas, int width, int height, int left, int top, SolidColorBrush color, FontFamily font, int fontSize, FontWeight bold, HorizontalAlignment align)
        private Label DrawLabel(Canvas canvas, Theme.LabelProperties prop)
        {
            Label label = new Label();
            //SolidColorBrush myBrush = new SolidColorBrush(color);
            /*
            label.Width = width;
            label.Height = height;
            label.Foreground = color;
            //label.Foreground = new SolidColorBrush(Colors.White);
            label.Margin = new Thickness(left, top, 0, 0);
            label.FontSize = fontSize;
            label.FontFamily = font;
            label.VerticalContentAlignment = System.Windows.VerticalAlignment.Top;
            
            label.FontWeight = FontWeights.Bold;
            
            switch(align) {
                case "left":
                    label.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                    break;
                case "center":
                    label.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
                    break;
                case "right":
                    label.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
                    break;
            }
            
            label.HorizontalContentAlignment = align;
            */

            label.Width = prop.width;
            label.Height = prop.height;
            label.Foreground = prop.fontColor;
            label.Margin = new Thickness(prop.left, prop.top, 0, 0);
            label.FontSize = prop.fontSize;
            label.FontFamily = prop.font;
            label.VerticalContentAlignment = System.Windows.VerticalAlignment.Top;
            
            label.FontWeight = prop.FontBold;
            label.FontStyle = prop.FontItalic;

            label.HorizontalContentAlignment = prop.TextAlign;

            Canvas.SetZIndex(label, 100);

            return label;
        }

        /*
        private void DrawSidepanel()
        {
            for (int i = 0; i < 10; i++)
            {
                DrawRectangle(sidepanel, 34, 20, 0, i * 22, Properties.Settings.Default.color1, 2, 1);
                DrawRectangle(sidepanel, 100, 20, 32, i * 22, Properties.Settings.Default.color2, 0, 2);
                DrawRectangle(sidepanel, 50, 20, 132, i * 22, Properties.Settings.Default.color3, 2, 1);

                DrawLabel(sidepanel, 34, 16, 2, i * 22, Properties.Settings.Default.color1, 11);
                DrawLabel(sidepanel, 100, 16, 34, i * 22, Properties.Settings.Default.color2, 12);
                DrawLabel(sidepanel, 50, 16, 134, i * 22, Properties.Settings.Default.color3, 11);
            }
        }
        */
    }
}