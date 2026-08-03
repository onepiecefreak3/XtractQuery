using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.Configuration.DataClasses;
using System.Globalization;

namespace CrossCutting.Core.Configuration;

public sealed class Configurator : IConfigurator
{
    private readonly IList<ConfigCategory> _categories;

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

        return _categories.Any(c => c.Name == category && c.Entries.Any(e => e.Key == key));
    }

    public T Get<T>(string category, string key, T defaultValue = default!)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentNullException(nameof(category));

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        ConfigCategory? configCategory = _categories.SingleOrDefault(c => c.Name == category);
        if (configCategory is null)
            return defaultValue;

        ConfigEntry? entry = configCategory.Entries.SingleOrDefault(e => e.Key == key);
        if (entry is null)
            return defaultValue;

        if (entry.Value is null)
            return defaultValue;

        if (entry.Value is T typed)
            return typed;

        Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (entry.Value is IConvertible)
            return (T)Convert.ChangeType(entry.Value, targetType, CultureInfo.InvariantCulture);

        throw new InvalidCastException(
            $"Cannot convert configuration value of type '{entry.Value.GetType().FullName}' to '{typeof(T).FullName}'.");
    }
}
