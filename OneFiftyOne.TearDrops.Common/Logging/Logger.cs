using OneFiftyOne.TearDrops.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneFiftyOne.TearDrops.Common.Logging
{
    public class Logger
    {
        private static volatile ILogProvider logger;
        private static object syncRoot = new Object();
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

        public static void Load(ILogProvider provider)
        {
            lock (syncRoot)
            {
                if (logger != null)
                    logger.Unload();

                logger = provider;
            }
        }
    }
}
