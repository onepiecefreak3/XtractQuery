using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.Configuration.DataClasses;
using CrossCutting.Core.Contract.Configuration.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrossCutting.Core.Configuration
{
    public sealed class Configurator : IConfigurator
    {
        #region Fields

        private readonly IList<ConfigCategory> _categories;

        #endregion

        public Configurator(IEnumerable<IConfigurationRepository> repositories)
        {
            _categories = repositories.SelectMany(x => x.Load()).ToArray();
        }

        public bool Contains(string category, string key)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentNullException(nameof(category));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            bool exists = _categories.Any(c => c.Name == category && c.Entries.Any(e => e.Key == key));
            return exists;
        }

        public T Get<T>(string category, string key)
        {
            if (!Contains(category, key))
                throw new KeyOrCategoryNotFoundException(category, key);

            T value = Get<T>(category, key, default(T));
            return value;
        }

        public T Get<T>(string category, string key, T defaultValue)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentNullException(nameof(category));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            ConfigCategory configCategory = _categories.SingleOrDefault(c => c.Name == category);
            if (configCategory == null)
                return defaultValue;

            ConfigEntry entry = configCategory.Entries.SingleOrDefault(e => e.Key == key);
            if (entry == null)
            {
                return defaultValue;
            }

            return (T)entry.Value;
        }
    }
}