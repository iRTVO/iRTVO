using iRSDKSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iRTVOPlayer
{
    class Program
    {
        static iRacingSDK sdk = null;

        static void Main(string[] args)
        {
            if (args.Length < 1)
                return;

            Bookmarks myBookmarks = new Bookmarks(); ;
            BookmarkEvent thisEvent = null;
            int currentIndex = 0;
            int CurrentFrame = 0;
            bool run = true;
            using (StreamReader sw = new StreamReader(args[0], Encoding.UTF8))
            {
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(myBookmarks.GetType());
                myBookmarks = x.Deserialize(sw) as Bookmarks;
            }

            Console.WriteLine("Waiting for iRacing to come up ....");
            sdk = new iRacingSDK();
            while (!Console.KeyAvailable && run)
            {
                //Check if the SDK is connected
                if (sdk.IsConnected())
                {
                    while (sdk.GetData("SessionNum") == null)
                    {
                        Console.WriteLine("Waiting for Session...");
                        Thread.Sleep(200); // Allow other windows to initialize more faster
                        
                    }
                    
                    thisEvent = myBookmarks.List[currentIndex];

                    switch (thisEvent.BookmarkType)
                    {
                        case BookmarkEventType.Start:
                            ReplaySeek( thisEvent );
                            currentIndex++;
                            break;
                        case BookmarkEventType.Play:                            
                            CurrentFrame = (Int32)sdk.GetData("ReplayFrameNum");
                            if (CurrentFrame < thisEvent.ReplayPos)
                                continue;
                            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, thisEvent.DriverIdx, thisEvent.CamIdx);
                            SetPlaySpeed(thisEvent.PlaySpeed);
                            currentIndex++;
                            break;
                        case BookmarkEventType.Stop:
                            CurrentFrame = (Int32)sdk.GetData("ReplayFrameNum");
                            if (CurrentFrame < thisEvent.ReplayPos)
                                continue;
                            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 0, 0);
                            Console.WriteLine("End");
                            run = false;
                            break;
                        default:
                            run = false;
                            break;
                    }
                            
                    
                }
                else
                {                    
                    if ( sdk.Startup() )
                    {
                        Console.WriteLine("iRacing up and running.");
                    }
                    else
                        Thread.Sleep(2000);
                }
            }
            sdk.Shutdown();
        }

        private static void ReplaySeek(BookmarkEvent ev)
        {
            Int32 rewindFrames = (Int32)sdk.GetData("ReplayFrameNum") - (int)ev.ReplayPos - (ev.Rewind * 60);

            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, 0, 0);
            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.CamSwitchNum, ev.DriverIdx, ev.CamIdx);

            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlayPosition, (int)iRSDKSharp.ReplayPositionModeTypes.Begin, (int)(ev.ReplayPos - (ev.Rewind * 60)));

            Int32 curpos = (Int32)sdk.GetData("ReplayFrameNum");
            DateTime timeout = DateTime.Now;

            // wait rewind to finish, but only 15 secs
            while (curpos != (int)(ev.ReplayPos - (ev.Rewind * 60)) && (DateTime.Now - timeout).TotalSeconds < 15)
            {
                Thread.Sleep(16);
                curpos = (Int32)sdk.GetData("ReplayFrameNum");
            }
            SetPlaySpeed(ev.PlaySpeed);            
        }

        private static void SetPlaySpeed(int playspeed)
        {
            int slomo = 0;           
            if (playspeed > 0)
                slomo = 1;
            else
            {
                playspeed = Math.Abs(playspeed);
            }
            sdk.BroadcastMessage(iRSDKSharp.BroadcastMessageTypes.ReplaySetPlaySpeed, playspeed, slomo);
        }

    }


}
