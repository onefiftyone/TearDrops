using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneFiftyOne.TearDrops.Common.Exceptions
{
    /// <summary>
    /// Class representing an exception in the Configuration Drop
    /// </summary>
    [Serializable]
    public class ConfigurationException : TearDropException
    {
        public ConfigurationException() { }
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception inner) : base(message, inner) { }

        protected ConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
