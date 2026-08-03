using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.Configuration.Exceptions;

namespace CrossCutting.Core.Configuration.ConfigObjects;

public class ConfigObjectProvider : IConfigObjectProvider
{
    private readonly IConfigurator _configurator;
    private readonly Dictionary<Type, object> _configObjects = new();
    private readonly object _sync = new();

    public ConfigObjectProvider(IConfigurator configurator)
    {
        _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
    }

    public TConfig Get<TConfig>()
    {
        return (TConfig)Get(typeof(TConfig));
    }

    public object Get(Type configType)
    {
        ArgumentNullException.ThrowIfNull(configType);

        lock (_sync)
        {
            if (_configObjects.TryGetValue(configType, out object? cached))
                return cached;

            if (!ConfigObjectFactoryRegistry.TryCreate(configType, _configurator, out object? created) || created is null)
            {
                throw new ConfigurationException(
                    $"No generated configuration factory registered for type '{configType.FullName}'. " +
                    "Ensure the type is annotated with [ConfigurationCategory] and the configuration source generator is referenced.");
            }

            _configObjects[configType] = created;
            return created;
        }
    }
}
