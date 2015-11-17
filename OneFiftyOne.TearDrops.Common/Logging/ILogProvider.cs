using OneFiftyOne.TearDrops.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneFiftyOne.TearDrops.Common.Logging
{
    public interface ILogProvider : IProvider
    {
        void Unload();
        void Log(LogMode mode, string message);
        void Log(LogMode mode, object message);
        void Log(LogMode mode, object message, Exception e);

        void Log(Type type, LogMode mode, string message);
        void Log(Type type, LogMode mode, object message);
        void Log(Type type, LogMode mode, object message, Exception e);
    }
}
