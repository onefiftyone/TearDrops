using OneFiftyOne.TearDrops.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Dynamic;
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

        /// <summary>
        /// Checks for string equality in the current culture, ignoring case.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="compare">The compare.</param>
        /// <returns></returns>
        public static bool EqualsIgnoreCase(this string input, string compare)
        {
            return input.Equals(compare, StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Takes a list of strings and combines them, delimited by the specified delimiter (default: ",")
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <returns></returns>
        public static string ToDelimitedList(this List<string> list, string delimiter = ",")
        {
            return string.Join(delimiter, list);
        }

        #endregion

        #region Object Extensions

        public static bool TryGetIndexer(this object obj, string index, out object result)
        {
            result = null;
            var getIndexMethod = obj.GetType().GetMethod("get_Item", new Type[] { typeof(string) });
            if (getIndexMethod == null)
                return false;

            result = getIndexMethod.Invoke(obj, new object[] { index });
            return true; 
        }

        public static bool TryGetIndexer(this object obj, int index, out object result)
        {
            result = null;
            var getIndexMethod = obj.GetType().GetMethod("get_Item", new Type[] { typeof(int) });
            if (getIndexMethod == null)
                return false;

            result = getIndexMethod.Invoke(obj, new object[] { index });
            return true;
        }

        public static bool TrySetIndexer(this object obj, string index, object value)
        {
            var methods = obj.GetType().GetMethods().Where(m => m.Name.Equals("set_Item") && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(string));
            if (methods.Count() != 1)
                throw new TearDropException("Unable to find set Indexer function that excepts a string ");
            var setIndexMethod = methods.FirstOrDefault();
            if (setIndexMethod == null)
                return false;

            setIndexMethod.Invoke(obj, new object[] { index, value });
            return true;
        }


        public static bool TrySetIndexer(this object obj, int index, object value)
        {
            var methods = obj.GetType().GetMethods().Where(m => m.Name.Equals("set_Item") && m.GetParameters().FirstOrDefault()?.ParameterType == typeof(int));
            if (methods.Count() != 1)
                return false;
            var setIndexMethod = methods.FirstOrDefault();
            if (setIndexMethod == null)
                return false;

            setIndexMethod.Invoke(obj, new object[] { index, value });
            return true;
        }

        public static ExpandoObject CreateExpando(this object item)
        {
            var dictionary = new ExpandoObject() as IDictionary<string, object>;
            foreach (var propertyInfo in item.GetType().GetProperties())
            {
                if (propertyInfo.GetIndexParameters().Length == 0)
                {
                    dictionary.Add(propertyInfo.Name.ToUpper(), propertyInfo.GetValue(item, null));
                }
            }
            return (ExpandoObject)dictionary;
        }

        #endregion
    }
}
