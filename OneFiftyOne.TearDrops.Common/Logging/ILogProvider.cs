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
    }
}
