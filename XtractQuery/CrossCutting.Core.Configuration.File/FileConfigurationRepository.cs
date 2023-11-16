using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.Configuration.DataClasses;
using CrossCutting.Core.Serialization.JsonAdapter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace CrossCutting.Core.Configuration.File
{
    public class FileConfigurationRepository : IConfigurationRepository
    {
        public IEnumerable<ConfigCategory> Load()
        {
            string cfgPath = GetConfigPath();
            if (!System.IO.File.Exists(cfgPath))
                yield break;

            string json = System.IO.File.ReadAllText(cfgPath);
            if (string.IsNullOrEmpty(json))
                yield break;

            JsonSerializer serializer = new();
            var result = serializer.Deserialize<List<ConfigCategory>>(json);

            foreach (ConfigCategory category in result)
            {
                foreach (ConfigEntry entry in category.Entries)
                    entry.Category = category;

                yield return category;
            }
        }

        private string GetConfigPath()
        {
            return Path.Combine(Environment.ProcessPath!, "config.json");
        }
    }
}
