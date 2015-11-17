using OneFiftyOne.TearDrops.Common.Configuration;
using OneFiftyOne.TearDrops.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Log(LogMode.Error, "Test error");
            Logger.Log(LogMode.Verbose, "Shouldnt show up");
        }
    }
}               
