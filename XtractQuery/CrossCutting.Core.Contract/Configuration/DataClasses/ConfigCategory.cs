using System.Collections.Generic;

namespace CrossCutting.Core.Contract.Configuration.DataClasses
{
    public class ConfigCategory
    {
        private readonly List<ConfigEntry> _entries;

        public string Name { get; set; }
        public IReadOnlyList<ConfigEntry> Entries => _entries;

        public ConfigCategory()
        {
            _entries = new List<ConfigEntry>();
        }

        public ConfigEntry AddEntry(string key, object value)
        {
            ConfigEntry result = new(this)
            {
                Key = key,
                Value = value
            };

            _entries.Add(result);

            return result;
        }
    }
}
