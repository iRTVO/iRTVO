/*
 * apiThread.cs
 * 
 * Gets data from API and stores it to SharedData
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
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace iRTVO
{
    public partial class Overlay : Window
    {

        // license colors to license
        Dictionary<string, string> license = new Dictionary<string, string>()
        {
            {"000000", "P"},
            {"0153db", "A"},
            {"00c702", "B"},
            {"feec04", "C"},
            {"fc8a27", "D"},
            {"fc0706", "R"}
        };

        // Comparer that sorts drivers according to their position on the track.
        public class StandingComparer : System.Collections.IComparer
        {
            public StandingComparer() { }

            public int Compare(object Object1, object Object2)
            {
                if (!(Object1 is SharedData.LapInfo))
                {
                    throw new ArgumentException(
                    "Object1 must be of type LapInfo.",
                    "Object1"
                    );
                }
                else if (!(Object2 is SharedData.LapInfo))
                {
                    throw new ArgumentException(
                    "Object2 must be of type LapInfo.",
                    "Object2"
                    );
                }

                int lap1 = ((SharedData.LapInfo)Object1).lapDiff;
                int lap2 = ((SharedData.LapInfo)Object2).lapDiff;

                if (lap1 > lap2)
                {
                    return 1;
                }
                else if (lap1 == lap2)
                {
                    float diff1 = ((SharedData.LapInfo)Object1).diff;
                    float diff2 = ((SharedData.LapInfo)Object2).diff;

                    if (diff1 > diff2)
                        return 1;
                    if (diff1 == diff2)
                        return 0;
                    else
                        return -1;
                }
                else // lap1 < lap 2
                {
                    return -1;
                }
            }
        }

        // Comparer that sorts drivers according to their fastest laptimes.
        public class LapTimeComparer : System.Collections.IComparer
        {
            public LapTimeComparer() { }

            public int Compare(object Object1, object Object2)
            {
                if (!(Object1 is SharedData.LapInfo))
                {
                    throw new ArgumentException(
                    "Object1 must be of type LapInfo.",
                    "Object1"
                    );
                }
                else if (!(Object2 is SharedData.LapInfo))
                {
                    throw new ArgumentException(
                    "Object2 must be of type LapInfo.",
                    "Object2"
                    );
                }

                float diff1 = ((SharedData.LapInfo)Object1).diff;
                float diff2 = ((SharedData.LapInfo)Object2).diff;

                if (diff1 > diff2)
                    return 1;
                else if (diff1 == diff2)
                    return 0;
                else
                    return -1;
            }
        }

        // Function that gets the data from the API
        void getData()
        {
            // init
            SharedData.driversMutex = new Mutex(true);
            SharedData.standingMutex = new Mutex(true);
            SharedData.sessionsMutex = new Mutex(true);
            SharedData.trackMutex = new Mutex(true);

            // init standings array
            for(int i = 0; i < iRacingTelem.MAX_SESSIONS; i++)
                SharedData.standing[i] = new SharedData.LapInfo[0];

            // current state of our connection
            //ConnectionState connectionState = ConnectionState.initializing;

            String[] connectionStateStr = { "initializing", "connecting", "active" };

            // how long to sleep for
            int timeOutMs = 33;

            //what messages are we asking the sim to give us
            iRacingTelem.eSimDataType[] desired = 
            {
	            iRacingTelem.eSimDataType.kSampleHeader,
	            iRacingTelem.eSimDataType.kSessionInfo,
	            iRacingTelem.eSimDataType.kCameraInfo,
	            iRacingTelem.eSimDataType.kDriverInfo,
	            iRacingTelem.eSimDataType.kLapInfo,
	            iRacingTelem.eSimDataType.kCurrentWeekendEx,
            };

            while (SharedData.runApi)
            {
                switch (SharedData.apiState)
                {
                    case iRTVO.SharedData.ConnectionState.initializing:
                        if (iRacingTelem.AppBegin("iRacing.com Simulator", IntPtr.Zero))
                            SharedData.apiState = iRTVO.SharedData.ConnectionState.connecting;
                        else
                            Thread.Sleep(timeOutMs);
                        break;
                    case iRTVO.SharedData.ConnectionState.connecting:
                        if (iRacingTelem.AppCheckIfSimActiveQ())
                        {
                            if (iRacingTelem.AppRequestDataItems(desired.GetLength(0), desired) &&
                                iRacingTelem.AppRequestDataAtPhysicsRate(false) &&
                                iRacingTelem.AppEnableSampling(false))
                            {
                                SharedData.apiState = iRTVO.SharedData.ConnectionState.active;
                            }
                            else
                                Thread.Sleep(timeOutMs);
                        }
                        else
                            Thread.Sleep(timeOutMs);
                        break;
                    case iRTVO.SharedData.ConnectionState.active:
                        SharedData.runOverlay = true;
                        if (iRacingTelem.AppCheckIfSimActiveQ())
                        {
                            iRacingTelem.eSimDataType newStateData = iRacingTelem.eSimDataType.kNoStateInfo;
                            if (iRacingTelem.AppWaitForNewSample(ref newStateData, timeOutMs) || newStateData != iRacingTelem.eSimDataType.kNoStateInfo)
                            {
                                if (newStateData != iRacingTelem.eSimDataType.kNoStateInfo)
                                {
                                    // It was state data.  Process it.
                                    {
                                        IntPtr pt = iRacingTelem.AppGetSimData(newStateData);
                                        if (pt != IntPtr.Zero)
                                        {
                                            // define our structs for use in the switch.
                                            iRacingTelem.SessionInfo si;
                                            iRacingTelem.CameraInfo ci;
                                            iRacingTelem.DriverInfo di;
                                            iRacingTelem.LapInfo li;
                                            iRacingTelem.CurrentWeekendEx ce;

                                            switch (newStateData)
                                            {
                                                case iRacingTelem.eSimDataType.kSessionInfo:
                                                    si = (iRacingTelem.SessionInfo)Marshal.PtrToStructure(pt, typeof(iRacingTelem.SessionInfo));

                                                    SharedData.sessionsMutex = new Mutex(true);

                                                    SharedData.currentSession = si.sessionNum;
                                                    SharedData.sessions[SharedData.currentSession].lapsRemaining = si.lapsRemaining;
                                                    SharedData.sessions[SharedData.currentSession].timeRemaining = si.timeRemaining;
                                                    SharedData.sessions[SharedData.currentSession].state = (iRacingTelem.eSessionState)si.sessionState;
                                                    SharedData.sessions[SharedData.currentSession].type = (iRacingTelem.eSessionType)si.sessionType;
                                                    SharedData.sessions[SharedData.currentSession].flag = (iRacingTelem.eSessionFlag)si.sessionFlag;

                                                    SharedData.sessionsMutex.ReleaseMutex();
                                                    //SharedData.sessionsUpdated = true;

                                                    break;
                                                case iRacingTelem.eSimDataType.kCameraInfo:
                                                    ci = (iRacingTelem.CameraInfo)Marshal.PtrToStructure(pt, typeof(iRacingTelem.CameraInfo));
                                                    if (ci.carIdx >= 0)
                                                    {                                                        
                                                        SharedData.sessionsMutex = new Mutex(true);
                                                        SharedData.sessions[SharedData.currentSession].driverFollowed = ci.carIdx;
                                                        SharedData.sessionsMutex.ReleaseMutex();
                                                        //SharedData.sessionsUpdated = true;
                                                    }
                                                    break;
                                                case iRacingTelem.eSimDataType.kDriverInfo:
                                                    di = (iRacingTelem.DriverInfo)Marshal.PtrToStructure(pt, typeof(iRacingTelem.DriverInfo));
                                                    foreach (iRacingTelem.DriverInfoRow driver in di.row)
                                                    {
                                                        if (driver.userID > 0 && driver.carNum != "ÿÿÿ")
                                                        {
                                                            SharedData.driversMutex = new Mutex(true);
                                                            if (driver.onTrack == false && SharedData.drivers[driver.carIdx].onTrack == true)
                                                                SharedData.drivers[driver.carIdx].offTrackSince = DateTime.Now;

                                                            SharedData.drivers[driver.carIdx].onTrack = driver.onTrack;
                                                            SharedData.drivers[driver.carIdx].carId = driver.carID;
                                                            SharedData.drivers[driver.carIdx].name = driver.userName;
                                                            SharedData.drivers[driver.carIdx].userId = driver.userID;
                                                            SharedData.drivers[driver.carIdx].club = driver.clubName;
                                                            SharedData.drivers[driver.carIdx].car = driver.carPath;
                                                            SharedData.drivers[driver.carIdx].carclass = driver.carClassID;
                                                            switch (driver.licLevel)
                                                            {
                                                                case 0:
                                                                case 1:
                                                                case 2:
                                                                case 3:
                                                                case 4:
                                                                    SharedData.drivers[driver.carIdx].license = "R" + ((double)driver.licSubLevel / 100).ToString("0.00");
                                                                    break;
                                                                case 5:
                                                                case 6:
                                                                case 7:
                                                                case 8:
                                                                    SharedData.drivers[driver.carIdx].license = "D" + ((double)driver.licSubLevel / 100).ToString("0.00");
                                                                    break;
                                                                case 9:
                                                                case 10:
                                                                case 11:
                                                                case 12:
                                                                    SharedData.drivers[driver.carIdx].license = "C" + ((double)driver.licSubLevel / 100).ToString("0.00");
                                                                    break;
                                                                case 14:
                                                                case 15:
                                                                case 16:
                                                                case 17:
                                                                    SharedData.drivers[driver.carIdx].license = "B" + ((double)driver.licSubLevel / 100).ToString("0.00");
                                                                    break;
                                                                case 18:
                                                                case 19:
                                                                case 20:
                                                                case 21:
                                                                    SharedData.drivers[driver.carIdx].license = "A" + ((double)driver.licSubLevel / 100).ToString("0.00");
                                                                    break;
                                                                case 22:
                                                                case 23:
                                                                case 24:
                                                                case 25:
                                                                    SharedData.drivers[driver.carIdx].license = "P" + ((double)driver.licSubLevel / 100).ToString("0.00");
                                                                    break;
                                                                case 26:
                                                                case 27:
                                                                case 28:
                                                                case 29:
                                                                    SharedData.drivers[driver.carIdx].license = "WC" + ((double)driver.licSubLevel / 100).ToString("0.00");
                                                                    break;
                                                                default:
                                                                    SharedData.drivers[driver.carIdx].license = "Unknown";
                                                                    break;
                                                            }

                                                            //SharedData.drivers[driver.carIdx].license = license[driver.licColor] +" "+ driver.licLevel + "." + driver.licSubLevel;
                                                            SharedData.drivers[driver.carIdx].numberPlate = Int32.Parse(driver.carNum);

                                                            string[] nameWords = driver.userName.Split(' ');

                                                            SharedData.drivers[driver.carIdx].shortname = nameWords[0].Substring(0, 1).ToUpper() + ' ' + nameWords[nameWords.Length - 1];

                                                            if (nameWords.Length == 2)
                                                            {
                                                                SharedData.drivers[driver.carIdx].initials = nameWords[0].Substring(0, 1).ToUpper() + nameWords[1].Substring(0, 2).ToUpper();
                                                            }
                                                            else
                                                            {
                                                                SharedData.drivers[driver.carIdx].initials = nameWords[0].Substring(0, 1).ToUpper() + nameWords[1].Substring(0, 1).ToUpper() + nameWords[nameWords.Length - 1].Substring(0, 1).ToUpper();
                                                            }
                                                            SharedData.driversMutex.ReleaseMutex();
                                                            //SharedData.driversUpdated = true;
                                                        }
                                                    }
                                                    break;
                                                case iRacingTelem.eSimDataType.kLapInfo:
                                                    li = (iRacingTelem.LapInfo)Marshal.PtrToStructure(pt, typeof(iRacingTelem.LapInfo));
                                                    SharedData.standingMutex = new Mutex(true);
                                                    foreach (iRacingTelem.LapInfoSession lapInfo in li.sessions)
                                                    {
                                                        int sessionNum = lapInfo.sessionNum;
                                                        int size = 0;
                                                        SharedData.LapInfo[] tmpStanding = new SharedData.LapInfo[iRacingTelem.MAX_CARS];

                                                        if (sessionNum >= 0 && SharedData.sessions[sessionNum].state != iRacingTelem.eSessionState.kSessionStateInvalid)
                                                        {

                                                            if (sessionNum == SharedData.currentSession && 
                                                                SharedData.sessions[sessionNum].state != iRacingTelem.eSessionState.kSessionStateCoolDown)
                                                            {
                                                                foreach (iRacingTelem.LapInfoEntry position in lapInfo.position)
                                                                {
                                                                    if (position.carIdx >= 0)
                                                                    {
                                                                        int id = 0;
                                                                        Boolean found = false;

                                                                        for (int k = 0; k < tmpStanding.Length; k++)
                                                                        {
                                                                            if (tmpStanding[k].id == position.carIdx)
                                                                            {
                                                                                id = k;
                                                                                found = true;
                                                                            }
                                                                        }

                                                                        if (!found)
                                                                            id = size;

                                                                        size++;

                                                                        tmpStanding[id].id = position.carIdx;
                                                                        tmpStanding[id].diff = position.resTime;
                                                                        tmpStanding[id].lapDiff = position.resLap;
                                                                        tmpStanding[id].completedLaps = position.lapsComplete;
                                                                        tmpStanding[id].fastLap = position.fastTime;
                                                                        SharedData.driversMutex = new Mutex(true);
                                                                        SharedData.drivers[position.carIdx].completedlaps = position.lapsComplete;
                                                                        SharedData.drivers[position.carIdx].fastestlap = position.fastTime;
                                                                        if (position.lapsComplete > SharedData.drivers[position.carIdx].lastNewLapNr)
                                                                        {
                                                                            SharedData.drivers[position.carIdx].lastNewLap = DateTime.Now;
                                                                            SharedData.drivers[position.carIdx].lastNewLapNr = position.lapsComplete;
                                                                            SharedData.drivers[position.carIdx].previouslap = position.lastTime;
                                                                        }
                                                                        SharedData.driversMutex.ReleaseMutex();
                                                                    }
                                                                }

                                                                if (size > 1)
                                                                {

                                                                    switch (SharedData.sessions[sessionNum].type)
                                                                    {
                                                                        case iRacingTelem.eSessionType.kSessionTypeRace:
                                                                            Array.Sort(tmpStanding, new StandingComparer());
                                                                            break;
                                                                        default:
                                                                            Array.Sort(tmpStanding, new LapTimeComparer());
                                                                            size = 0;
                                                                            for (int k = 0; k < tmpStanding.Length; k++)
                                                                            {
                                                                                if (tmpStanding[k].lapDiff > 0)
                                                                                    size++;
                                                                            }
                                                                            break;
                                                                    }

                                                                    SharedData.standing[sessionNum] = new SharedData.LapInfo[size];
                                                                    Array.Copy(tmpStanding, tmpStanding.Length - size, SharedData.standing[sessionNum], 0, size);
                                                                    // copy leader as his diff is negative and we are in race
                                                                    if (SharedData.sessions[sessionNum].type == iRacingTelem.eSessionType.kSessionTypeRace)
                                                                        Array.Copy(tmpStanding, 0, SharedData.standing[sessionNum], 0, 1);

                                                                    //SharedData.standingsUpdated = true;
                                                                }

                                                            }
                                                        }
                                                    }
                                                    SharedData.standingMutex.ReleaseMutex();
                                                    break;
                                                case iRacingTelem.eSimDataType.kCurrentWeekendEx:
                                                    ce = (iRacingTelem.CurrentWeekendEx)Marshal.PtrToStructure(pt, typeof(iRacingTelem.CurrentWeekendEx));
                                                    int j = 0;
                                                    SharedData.sessionsMutex = new Mutex(true);
                                                    foreach (iRacingTelem.SessionParam sessionInfo in ce.sessions)
                                                    {
                                                        SharedData.sessions[j].laps = sessionInfo.laps;
                                                        SharedData.sessions[j].time = sessionInfo.length;
                                                        j++;
                                                    }
                                                    SharedData.sessionsMutex.ReleaseMutex();
                                                    //SharedData.sessionsUpdated = true;

                                                    SharedData.trackMutex = new Mutex(true);

                                                    SharedData.track.id = ce.trackID;
                                                    SharedData.track.name = ce.track;
                                                    SharedData.track.length = ce.trackLength;

                                                    SharedData.trackMutex.ReleaseMutex();
                                                    break;
                                            }


                                        }
                                    }
                                }
                            }

                            if (iRacingTelem.AppCheckIfSimDataOverrunQ())
                            {
                                iRacingTelem.AppClearSimDataOverrun();
                            }
                        }
                        else
                        {
                                iRacingTelem.AppEnd();
                                SharedData.apiState = iRTVO.SharedData.ConnectionState.initializing;
                        }
                        break;
                };
            }
            iRacingTelem.AppEnd();
        }
    }
}