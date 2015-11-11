using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneFiftyOne.TearDrops.Common.Exceptions
{

    /// <summary>
    /// Class representing a Generic TearDrop Exception.
    /// </summary>
    [Serializable]
    public class TearDropException : Exception
    {
        public TearDropException() { }
        public TearDropException(string message) : base(message) { }
        public TearDropException(string message, Exception inner) : base(message, inner) { }

        protected TearDropException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
