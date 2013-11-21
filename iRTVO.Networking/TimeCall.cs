using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLog;

namespace iRTVO.Networking
{
    public class TimeCall : IDisposable
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static int WarnThreshold = 5000;

        private string MethodName;
        private int StartTicks;

        public TimeCall(string MethodName)
        {
            this.MethodName = MethodName;
            StartTicks = Environment.TickCount;
        }

        public void Dispose()
        {
            if (LogManager.IsLoggingEnabled())
            {
                int time = (Environment.TickCount - StartTicks);
                if (time > WarnThreshold)
                    logger.Warn("{0} took {1} ms", MethodName, time / 1000);
                else
                    logger.Trace("{0} took  {1} ms", MethodName,  time / 1000);
                
            }
        }
    }
}
