using System;
using CrossCutting.Core.Contract.Aspects;
using CrossCutting.Core.Contract.Configuration.Exceptions;

namespace CrossCutting.Core.Contract.Configuration
{
    /// <summary>
    /// Contract to get or set config values from a store
    /// by a key/category pair
    /// </summary>
    [MapException(typeof(ConfigurationException))]
    public interface IConfigurator
    {
        /// <summary>
        /// Checks that a config value exists in the configurator
        /// </summary>
        /// <param name="category">The category the value is stored in</param>
        /// <param name="key">The key under the value is stored</param>
        /// <returns>If the value exists in the store</returns>
        /// <exception cref="ArgumentException">If the passed category or key is null, empty or whitespace</exception>
        bool Contains(string category, string key);

        /// <summary>
        /// Gets a config value from the configurator
        /// </summary>
        /// <typeparam name="T">The expected return type</typeparam>
        /// <param name="category">The category the value is stored in</param>
        /// <param name="key">The key under the value is stored</param>
        /// <param name="defaultValue">The default value, if category/key is not found.</param>
        /// <returns>The value from the store, or the default value if passed as a parameter</returns>
        /// <exception cref="KeyOrCategoryNotFoundException">If the key or the category was not found</exception>
        /// <exception cref="ArgumentException">If the passed category or key is null, empty or whitespace</exception>
        /// <exception cref="InvalidCastException">If the expected return type differs from the stored type</exception>
        T Get<T>(string category, string key, T defaultValue = default(T));
    }
}
