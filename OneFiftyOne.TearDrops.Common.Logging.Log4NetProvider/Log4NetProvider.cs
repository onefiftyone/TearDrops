using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OneFiftyOne.TearDrops.Common.Configuration;

namespace OneFiftyOne.TearDrops.Common.Logging
{
    public class Log4NetProvider : ILogProvider
    {
        public string ConfigSectionName { get { return "Log4NetSettings"; } }
        public bool Enabled { get; set; }

        public void Init(Settings settings)
        {
            //Setup Settings
            var xmlString = settings.ToXML();

            //Load into Logger
            Logger.Load(this);
        }

        public void Unload()
        {
         //nothing
        }
    }
}
