using OneFiftyOne.TearDrops.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneFiftyOne.TearDrops.Common
{
    public interface IProvider
    {
        bool Enabled { get; set; }
        string ConfigSectionName { get; }
        void Init(Settings settings);
    }
}
