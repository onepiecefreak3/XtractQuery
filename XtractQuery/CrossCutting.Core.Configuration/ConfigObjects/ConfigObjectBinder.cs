using System.Globalization;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.Configuration.Exceptions;

namespace CrossCutting.Core.Configuration.ConfigObjects;

/// <summary>
/// Helpers used by source-generated configuration factories.
/// </summary>
public static class ConfigObjectBinder
{
    public static T GetRequired<T>(IConfigurator configurator, string category, params string[] keys)
    {
        if (TryGet(configurator, category, out T value, keys))
            return value;

        string keyLabel = keys.Length == 0 ? "<missing>" : string.Join("' / '", keys);
        throw new RequiredConfigValueMissingException(category, keyLabel);
    }

    public static bool TryGet<T>(IConfigurator configurator, string category, out T value, params string[] keys)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(keys);

        foreach (string key in keys)
        {
            if (!configurator.Contains(category, key))
                continue;

            object? raw = configurator.Get<object?>(category, key);
            value = ConvertValue<T>(raw);
            return true;
        }

        value = default!;
        return false;
    }

    public static void AssignIfPresent<T>(IConfigurator configurator, string category, Action<T> assign, params string[] keys)
    {
        if (TryGet(configurator, category, out T value, keys))
            assign(value);
    }

    private static T ConvertValue<T>(object? raw)
    {
        if (raw is null)
            return default!;

        if (raw is T typed)
            return typed;

        Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType.IsEnum)
            return (T)Enum.Parse(targetType, raw.ToString()!, ignoreCase: true);

        if (raw is IConvertible)
            return (T)Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);

        throw new InvalidCastException($"Cannot convert configuration value of type '{raw.GetType().FullName}' to '{typeof(T).FullName}'.");
    }
}
