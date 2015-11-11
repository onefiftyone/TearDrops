using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneFiftyOne.TearDrops.Common
{
    public static class ExtensionMethods
    {
        #region String Extensions

        /// <summary>
        /// Returns true if string has a value or false if it is null or empty.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <returns></returns>
        public static bool HasValue(this string val)
        {
            return !string.IsNullOrEmpty(val);
        }

        #endregion
    }
}
