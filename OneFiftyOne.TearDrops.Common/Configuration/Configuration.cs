using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OneFiftyOne.TearDrops.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;

namespace OneFiftyOne.TearDrops.Common.Configuration
{
    public static class Configuration
    {
        private static readonly string defaultConfigFile = "TearDrops.json";

        static Configuration()
        {
            var defaultFile = Path.Combine(Environment.CurrentDirectory, defaultConfigFile);
            if (!File.Exists(defaultFile))
                return;

            ConfigFile = defaultFile;
            Load(defaultConfigFile);
        }

        public static void Load(string configFile)
        {
            ConfigFile = string.Empty;
            try
            {
                using (StreamReader file = File.OpenText(configFile))
                using (JsonTextReader tr = new JsonTextReader(file))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    Settings = serializer.Deserialize<Settings>(tr);
                }

                //auto init providers
                autoInitProviders(Settings);
            }
            catch (Exception e)
            {
                throw new ConfigurationException($"Unable to load configuration file: {configFile}.", e);
            }

            ConfigFile = configFile;
        }

        private static void autoInitProviders(Settings settings)
        {
            foreach(var pair in settings)
            {
                if (pair.Value is JObject)
                {
                    autoInitProviders(((JObject)pair.Value).ToObject<Settings>());
                    continue;
                }

                if(pair.Key.Equals("Provider"))
                {
                    var enabled = settings["Enabled"] != null && ((bool)settings["Enabled"] == true);
                    if(enabled)
                    {
                        IProvider provider = null;
                        try
                        {
                            provider = (IProvider)Activator.CreateInstance(Type.GetType((string)pair.Value, true, true));
                        }
                        catch(Exception e)
                        {
                            throw new ConfigurationException($"Unable to locate 'Provider' type specified: {pair.Value}", e);
                        }

                        if(provider==null)
                            throw new ConfigurationException($"Unable to activate 'Provider' type specified: {pair.Value}");

                        var providerSettings = settings[provider.ConfigSectionName] as Settings ?? new Settings();
                        provider.Init(providerSettings);
                    }
                }
            }
        }

        public static void Save()
        {
            if (!ConfigFile.HasValue())
                throw new ConfigurationException("No configuration file specified");

            SaveAs(ConfigFile);
        }

        public static void SaveAs(string filename)
        {
            using (StreamWriter file = File.CreateText(filename))
            { 
                var json = JsonConvert.SerializeObject((ExpandoObject)(Settings ?? new object()), Formatting.Indented);
                file.Write(json);  
            }
        }

        public static string ConfigFile { get; private set; }
        public static dynamic Settings { get; private set; }
    }
}
