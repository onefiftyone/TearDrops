using OneFiftyOne.TearDrops.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneFiftyOne.TearDrops.Common.Logging
{
    [Flags]
    public enum LogMode
    {
        Disabled = 0,
        Fatal = 1,
        Error = 2,
        Warning = 4,
        Info = 8,
        Verbose = 16
    }

    public class Logger
    {
        private static volatile ILogProvider logger;
        private static object syncRoot = new Object();
        private static string configSectionName = "LoggingDrop";
        private Logger() { }

        public static bool Enabled
        {
            get
            {
                return (logger != null && logger.Enabled);
            }
        }

        public static ILogProvider Provider
        {
            get
            {
                if (logger == null)
                    throw new TearDropException("Logger has not been initialized. Either add logging configuration, or call 'Load' manually.");

                return logger;
            }
        }

        public static LogMode Mode { get; set; }

        public static void Load(ILogProvider provider)
        {
            lock (syncRoot)
            {
                if (logger != null)
                    logger.Unload();
                logger = provider;
            }

            var loggingMode = (string)Configuration.Configuration.Settings["TearDrops"][configSectionName].Mode;

            LogMode mode;
            if (!Enum.TryParse<LogMode>(loggingMode, out mode))
                mode = LogMode.Warning; //default

            Mode = mode;
        }

        public static void Log(LogMode mode, string message)
        {
            if (Enabled && Mode != LogMode.Disabled && mode <= Mode)
                Provider.Log(mode, message);
        }
    }
}
