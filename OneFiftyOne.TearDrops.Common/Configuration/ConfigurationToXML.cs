using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneFiftyOne.TearDrops.Common.Configuration
{
    internal class ConfigurationToXML
    {
        public static string GetConfigXML(Settings input, bool includeRoot = false)
        {
            StringBuilder sb = new StringBuilder();

            if (includeRoot)
                sb.Append($"<{input.Name}>");

            foreach(var key in input.Keys)
                sb.Append(writeNodeRecursive(key, input[key]));

            if (includeRoot)
                sb.Append($"</{input.Name}>");

            return sb.ToString();
        }

        private static string writeNodeRecursive(string name, object value)
        {
            if (name.StartsWith("-"))
                return string.Empty;

            string attributes = string.Empty;
            StringBuilder children = new StringBuilder();
            if (value is Settings)
            {
                var settings = (Settings)value;
                var props = settings.Keys.Where(k => k.StartsWith("-"));
                attributes = string.Join(" ", props.Select(p => $"{p.TrimStart('-')}=\"{settings[p]}\""));

                foreach (var key in settings.Keys)
                    children.Append(writeNodeRecursive(key, settings[key]));
            }
            else
                children.Append(value.ToString());

            return $"<{name}{(attributes.HasValue() ? " " : string.Empty)}{attributes}>{children.ToString()}</{name}>";
        }
    }
}
