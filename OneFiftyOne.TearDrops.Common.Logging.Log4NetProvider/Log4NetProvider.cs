using log4net;
using OneFiftyOne.TearDrops.Common.Configuration;
using OneFiftyOne.TearDrops.Common.Exceptions;
using System;
using System.Xml;

namespace OneFiftyOne.TearDrops.Common.Logging
{
    public class Log4NetProvider : ILogProvider
    {
        private static readonly ILog defaultLogger = LogManager.GetLogger("default");

        public string ConfigSectionName { get { return "Log4NetSettings"; } }
        public bool Enabled { get; set; }

        public void Init(Settings settings)
        {
            //Setup Settings
            string xmlString = string.Empty;
            try
            {
                xmlString = settings.ToXML();
                var configDoc = new XmlDocument();
                configDoc.LoadXml(xmlString);
                log4net.Config.XmlConfigurator.Configure(configDoc.DocumentElement);
            }
            catch (Exception e)
            {
                throw new ConfigurationException($"Log4NetProvider - Unable to load configuration data. Configuration: {xmlString ?? string.Empty} ", e);
            }

            //Inject into Logger
            Logger.Load(this);
        }

        public void Log(LogMode mode, string message)
        {
            LogInternal(defaultLogger, mode, message);
        }

        public void Log(LogMode mode, object message)
        {
            LogInternal(defaultLogger, mode, message);
        }

        public void Log(LogMode mode, object message, Exception e)
        {
            LogInternal(defaultLogger, mode, message, e);
        }

        public void Log(Type type, LogMode mode, string message)
        {
            var logger = LogManager.GetLogger(type);
            LogInternal(logger, mode, message);
        }

        public void Log(Type type, LogMode mode, object message)
        {
            var logger = LogManager.GetLogger(type);
            LogInternal(logger, mode, message);
        }

        public void Log(Type type, LogMode mode, object message, Exception e)
        {
            var logger = LogManager.GetLogger(type);
            LogInternal(logger, mode, message, e);
        }

        protected void LogInternal(ILog logger, LogMode mode, object message)
        {
            switch (mode)
            {
                case LogMode.Disabled:
                    return;
                case LogMode.Fatal:
                    logger.Fatal(message);
                    break;
                case LogMode.Error:
                    logger.Error(message);
                    break;
                case LogMode.Warning:
                    logger.Warn(message);
                    break;
                case LogMode.Info:
                    logger.Info(message);
                    break;
                case LogMode.Verbose:
                    logger.Debug(message);
                    break;
            }
        }

        protected void LogInternal(ILog logger, LogMode mode, object message, Exception e)
        {
            switch (mode)
            {
                case LogMode.Disabled:
                    return;
                case LogMode.Fatal:
                    logger.Fatal(message, e);
                    break;
                case LogMode.Error:
                    logger.Error(message, e);
                    break;
                case LogMode.Warning:
                    logger.Warn(message, e);
                    break;
                case LogMode.Info:
                    logger.Info(message, e);
                    break;
                case LogMode.Verbose:
                    logger.Debug(message, e);
                    break;
            }
        }

        public void Unload()
        {
            //nothing
        }

    }
}
