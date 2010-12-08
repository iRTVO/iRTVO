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

        enum ConnectionState
        {
            initializing = 0,
            connecting,
            active,
        }

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

        /*
        void UpdateWindow(Label label, string message)
        {
            Action action = () => label.Content = message;
            Dispatcher.Invoke(action);
        }

        void UpdateSessionTypes(iRacingTelem.eSessionType[] SharedData.sessions)
        {
            Action action = () => sessionTypes = SharedData.sessions;
            Dispatcher.Invoke(action);
        }

        void SetLastPage()
        {
            Action action = () => resultLastPage = true;
            Dispatcher.Invoke(action);
        }

        int GetState(int variable)
        {
            int output = -1;
            Action action = () => output = variable;
            Dispatcher.Invoke(action);

            return output;
        }
        */
        void getData()
        {
            SharedData.driversMutex = new Mutex(true);
            SharedData.standingMutex = new Mutex(true);
            SharedData.sessionsMutex = new Mutex(true);

            // current state of our connection
            ConnectionState connectionState = ConnectionState.initializing;

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
                switch (connectionState)
                {
                    case ConnectionState.initializing:
                        if (iRacingTelem.AppBegin("iRacing.com Simulator", IntPtr.Zero))
                            connectionState = ConnectionState.connecting;
                        else
                            Thread.Sleep(timeOutMs);
                        break;
                    case ConnectionState.connecting:
                        if (iRacingTelem.AppCheckIfSimActiveQ())
                        {
                            if (iRacingTelem.AppRequestDataItems(desired.GetLength(0), desired) &&
                                iRacingTelem.AppRequestDataAtPhysicsRate(false) &&
                                iRacingTelem.AppEnableSampling(false))
                            {
                                connectionState = ConnectionState.active;
                            }
                            else
                                Thread.Sleep(timeOutMs);
                        }
                        else
                            Thread.Sleep(timeOutMs);
                        break;
                    case ConnectionState.active:
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
                                                    SharedData.sessions[SharedData.currentSession].lapsRemaining = si.lapsRemaining - 1;
                                                    SharedData.sessions[SharedData.currentSession].timeRemaining = si.timeRemaining;
                                                    SharedData.sessions[SharedData.currentSession].state = (iRacingTelem.eSessionState)si.sessionState;
                                                    SharedData.sessions[SharedData.currentSession].type = (iRacingTelem.eSessionType)si.sessionType;

                                                    SharedData.sessionsMutex.ReleaseMutex();
                                                    //SharedData.sessionsMutexEvent.Set();

                                                    break;
                                                case iRacingTelem.eSimDataType.kCameraInfo:
                                                    ci = (iRacingTelem.CameraInfo)Marshal.PtrToStructure(pt, typeof(iRacingTelem.CameraInfo));
                                                    if (ci.carIdx >= 0)
                                                    {
                                                        //followedDriver = ci.carIdx;
                                                        
                                                        SharedData.sessionsMutex = new Mutex(true);
                                                        SharedData.sessions[SharedData.currentSession].driverFollowed = ci.carIdx;
                                                        SharedData.sessionsMutex.ReleaseMutex();
                                                        //SharedData.sessionsMutexEvent.Set();
                                                    }
                                                    break;
                                                case iRacingTelem.eSimDataType.kDriverInfo:
                                                    di = (iRacingTelem.DriverInfo)Marshal.PtrToStructure(pt, typeof(iRacingTelem.DriverInfo));
                                                    foreach (iRacingTelem.DriverInfoRow driver in di.row)
                                                    {
                                                        if (driver.userID > 0)
                                                        {
                                                            SharedData.driversMutex = new Mutex(true);
                                                            SharedData.drivers[driver.carIdx].carId = driver.carID;
                                                            SharedData.drivers[driver.carIdx].name = driver.userName;
                                                            SharedData.drivers[driver.carIdx].userId = driver.userID;
                                                            SharedData.drivers[driver.carIdx].onTrack = driver.onTrack;

                                                            string[] nameWords = driver.userName.Split(' ');
                                                            if (nameWords.Length == 2)
                                                            {
                                                                SharedData.drivers[driver.carIdx].initials = nameWords[0].Substring(0, 1).ToUpper() + nameWords[1].Substring(0, 2).ToUpper();
                                                            }
                                                            else
                                                            {
                                                                SharedData.drivers[driver.carIdx].initials = nameWords[0].Substring(0, 1).ToUpper() + nameWords[1].Substring(0, 1).ToUpper() + nameWords[nameWords.Length - 1].Substring(0, 1).ToUpper();
                                                            }
                                                            SharedData.driversMutex.ReleaseMutex();
                                                            //SharedData.driversMutexEvent.Set();
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

                                                        if (sessionNum == SharedData.currentSession)
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

                                                            }

                                                        }
                                                    }
                                                    SharedData.standingMutex.ReleaseMutex();
                                                    //SharedData.standingMutexEvent.Set();
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
                                                    //SharedData.sessionsMutexEvent.Set();
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
                            connectionState = ConnectionState.initializing;
                        }
                        break;
                };
            }
            iRacingTelem.AppEnd();
        }
    }
}