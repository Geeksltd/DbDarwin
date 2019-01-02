using NLog;
using System;

namespace DbDarwin.Common
{

    public class LogService
    {
        private static readonly Logger MyLogger = NLog.LogManager.GetCurrentClassLogger();

        public static void Error(Exception e, string v)
        {
            MyLogger.Error(e + "\r\n" + v);
        }

        public static void Error(string v)
        {
            MyLogger.Error(v);
        }

        public static void Error(Exception exception)
        {
            MyLogger.Error(exception);
        }

        public static void Warning(string exception)
        {
            MyLogger.Error(exception);
        }

        public static void Info(string v)
        {
            MyLogger.Info(v);
        }
    }
}
