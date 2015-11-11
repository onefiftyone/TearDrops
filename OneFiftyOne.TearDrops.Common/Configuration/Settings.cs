using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Collections;
using OneFiftyOne.TearDrops.Common.Exceptions;
using Newtonsoft.Json;

namespace OneFiftyOne.TearDrops.Common.Configuration
{
    public class Settings : DynamicObject
    {
        private ConcurrentDictionary<string, object> settings = new ConcurrentDictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
        private string name = "{root}";

        [JsonIgnore]
        public string Name { get; private set; }
          
        public List<string> Keys { get { return settings.Keys.ToList(); } }
        public List<object> Values { get { return settings.Values.ToList(); } }

        public Settings()
        {
        }

        public object this[string key]
        {
            get { return getValue(key); }
            set { settings[key] = value; }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            settings.TryGetValue(binder.Name, out result);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            settings[binder.Name] = value;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Count() != 1)
                throw new ArgumentException("Settings requires a single indexer");
            if (!(indexes[0] is string))
                throw new ArgumentException("Indexer must be a string");

            result = getValue((string)indexes[0]);
            return true;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return settings.GetEnumerator();
        }

        public static explicit operator ExpandoObject(Settings input)
        {
            var expando = new ExpandoObject() as IDictionary<string, object>;
            foreach (var pair in input.settings)
                expando.Add(pair);

            return (ExpandoObject)expando;
        }

        public string ToXML()
        {
            try
            {
                return ConfigurationToXML.GetConfigXML(this);
            }
            catch(Exception e)
            {
                throw new ConfigurationException("Unable to convert configuration data to XML", e);
            }
        }

        private object getValue(string key)
        {
            object value;
            settings.TryGetValue(key, out value);

            if (value is JToken)
            {
                Settings newSettings = ((JToken)value).ToObject<Settings>();
                newSettings.Name = key;
                return newSettings;
            }
            else if (value is JArray)
            {
                var array = (JArray)value;
                if (array.Count > 0)
                {
                    if (array[0] is JObject)
                        return array.Select(token =>
                        {
                            Settings newSettings = token.ToObject<Settings>();
                            newSettings.Name = key;
                            return newSettings;
                        });
                    else
                        return array.Select(token => ((JValue)token).Value);
                }
                else
                    return new Settings { Name = key };
            }
            else
                return value;
        }
    }
}
