using OneFiftyOne.TearDrops.Common.Configuration;
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
            //Configuration.Load(Configuration.ConfigFile);


            dynamic t = Configuration.Settings.setting1;

            //Configuration.Settings.setting1 = "test2";
            //Configuration.Save();
        }
    }
}               
