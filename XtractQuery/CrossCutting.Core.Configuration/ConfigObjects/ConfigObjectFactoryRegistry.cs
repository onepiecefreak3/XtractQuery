using System.Collections.Concurrent;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.Configuration.Exceptions;

namespace CrossCutting.Core.Configuration.ConfigObjects;

public static class ConfigObjectFactoryRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<IConfigurator, object>> Factories = new();

    public static void Register(Type configType, Func<IConfigurator, object> factory)
    {
        ArgumentNullException.ThrowIfNull(configType);
        ArgumentNullException.ThrowIfNull(factory);
        Factories[configType] = factory;
    }

    public static void Register<TConfig>(Func<IConfigurator, TConfig> factory)
        where TConfig : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Factories[typeof(TConfig)] = configurator => factory(configurator);
    }

    public static bool TryCreate(Type configType, IConfigurator configurator, out object? config)
    {
        ArgumentNullException.ThrowIfNull(configType);
        ArgumentNullException.ThrowIfNull(configurator);

        if (Factories.TryGetValue(configType, out Func<IConfigurator, object>? factory))
        {
            config = factory(configurator);
            return true;
        }

        config = null;
        return false;
    }

    public static TConfig Create<TConfig>(IConfigurator configurator)
        where TConfig : class
    {
        if (!TryCreate(typeof(TConfig), configurator, out object? config))
        {
            throw new ConfigurationException(
                $"No generated configuration factory registered for type '{typeof(TConfig).FullName}'. " +
                "Ensure the type is annotated with [ConfigurationCategory] and the configuration source generator is referenced.");
        }

        return (TConfig)config!;
    }
}
