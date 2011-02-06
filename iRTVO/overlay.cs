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

namespace iRTVO
{
    public partial class Overlay : Window
    {

        Canvas driver;
        Label driverPosLabel;
        Label driverNameLabel;
        Label driverDiffLabel;
        Label driverInfoLabel;

        Canvas sidepanel;
        Label[] sidepanelPosLabel;
        Label[] sidepanelNameLabel; 
        Label[] sidepanelDiffLabel;
        Label[] sidepanelInfoLabel;

        Canvas results;
        Label resultsHeader;
        Label resultsSubHeader;
        Label[] resultsPosLabel;
        Label[] resultsNameLabel;
        Label[] resultsDiffLabel;
        Label[] resultsInfoLabel;

        // ticker;
        StackPanel ticker;
        Label[] tickerPosLabel;
        Label[] tickerNameLabel;
        Label[] tickerDiffLabel;
        Label[] tickerInfoLabel;

        // sessionstate;
        Label sessionstateText;

        // laptime
        Label laptimeText;

        // ligths
        TimeSpan timer;

        // flags
        int[] flags = new int[4] { 
            (int)Theme.overlayTypes.flaggreen,
            (int)Theme.overlayTypes.flagyellow,
            (int)Theme.overlayTypes.flagwhite,
            (int)Theme.overlayTypes.flagcheckered
        };

        // fps counter
        Stopwatch stopwatch = Stopwatch.StartNew();
        DateTime drawBegun = DateTime.Now;

        private void overlayUpdate(object sender, EventArgs e)
        {

            stopwatch.Restart();
            SharedData.overlayFPS = DateTime.Now - drawBegun;
            drawBegun = DateTime.Now;

            if (SharedData.requestRefresh == true)
            {
                loadTheme(Properties.Settings.Default.theme);

                overlay.Left = Properties.Settings.Default.OverlayLocationX;
                overlay.Top = Properties.Settings.Default.OverlayLocationY;
                overlay.Width = Properties.Settings.Default.OverlayWidth;
                overlay.Height = Properties.Settings.Default.OverlayHeight;

                resizeOverlay(overlay.Width, overlay.Height);
                SharedData.requestRefresh = false;
            }

            if (SharedData.runOverlay)
            {

                // wait
                SharedData.driversMutex.WaitOne(5);
                SharedData.standingMutex.WaitOne(5);
                SharedData.sessionsMutex.WaitOne(5);

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
                    driverNameLabel.Content = String.Format(theme.driver.Name.text, theme.getFormats(SharedData.drivers[SharedData.sessions[SharedData.currentSession].driverFollowed]));
                    driverInfoLabel.Content = String.Format(theme.driver.Info.text, theme.getFormats(SharedData.drivers[SharedData.sessions[SharedData.currentSession].driverFollowed]));
                    if (SharedData.standing[SharedData.currentSession] != null)
                    {
                        for (int i = 0; i < SharedData.standing[SharedData.currentSession].Length; i++)
                        {
                            // update  driver
                            if (SharedData.standing[SharedData.currentSession][i].id == SharedData.sessions[SharedData.currentSession].driverFollowed)
                            {
                                noLapsDriver = false;
                                driverPosLabel.Content = String.Format(theme.driver.Num.text, theme.getFormats(SharedData.drivers[SharedData.sessions[SharedData.currentSession].driverFollowed], i));

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
                                            driverDiffLabel.Content = "-.--";
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
                                    sidepanelPosLabel[j].Content = String.Format(theme.sidepanel.Num.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id], k));
                                    sidepanelNameLabel[j].Content = String.Format(theme.sidepanel.Name.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id]));
                                    sidepanelInfoLabel[j].Content = String.Format(theme.sidepanel.Info.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][k].id]));

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
                            sidepanelPosLabel[i].Content = String.Format(theme.sidepanel.Num.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id], i));
                            sidepanelNameLabel[i].Content = String.Format(theme.sidepanel.Name.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));
                            sidepanelInfoLabel[i].Content = String.Format(theme.sidepanel.Info.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));

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
                            sidepanelPosLabel[i].Content = String.Format(theme.sidepanel.Num.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id], i));
                            sidepanelNameLabel[i].Content = String.Format(theme.sidepanel.Name.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));
                            sidepanelInfoLabel[i].Content = String.Format(theme.sidepanel.Info.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));
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
                            resultsPosLabel[j].Content = String.Format(theme.results.Num.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.resultSession][i].id], i));
                            resultsNameLabel[j].Content = String.Format(theme.results.Name.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.resultSession][i].id]));
                            resultsInfoLabel[j].Content = String.Format(theme.results.Info.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.resultSession][i].id]));


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
                        else if (timer.TotalSeconds < 9)
                        {
                            themeImages[(int)Theme.overlayTypes.lightsoff].Visibility = System.Windows.Visibility.Hidden;
                            themeImages[(int)Theme.overlayTypes.lightsred].Visibility = System.Windows.Visibility.Visible;
                            themeImages[(int)Theme.overlayTypes.lightsgreen].Visibility = System.Windows.Visibility.Hidden;
                        }
                        else
                        {
                            themeImages[(int)Theme.overlayTypes.lightsoff].Visibility = System.Windows.Visibility.Hidden;
                            themeImages[(int)Theme.overlayTypes.lightsred].Visibility = System.Windows.Visibility.Hidden;
                            themeImages[(int)Theme.overlayTypes.lightsgreen].Visibility = System.Windows.Visibility.Visible;
                        }
                    }
                }

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

                            tickerPosLabel[i].Content = String.Format(theme.ticker.Num.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id], i));
                            tickerNameLabel[i].Content = String.Format(theme.ticker.Name.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));
                            tickerInfoLabel[i].Content = String.Format(theme.ticker.Info.text, theme.getFormats(SharedData.drivers[SharedData.standing[SharedData.currentSession][i].id]));


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
                    laptimeText.Content = String.Format(theme.laptimeText.text, theme.getFormats(SharedData.drivers[SharedData.sessions[SharedData.currentSession].driverFollowed]));
                }
            }

            stopwatch.Stop();
            SharedData.overlayEffectiveFPS = stopwatch.Elapsed;

        }

        public static string floatTime2String(float time, Boolean showMilli, Boolean showMinutes)
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
        
    }
}