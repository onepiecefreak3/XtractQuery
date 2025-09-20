using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.Configuration.DataClasses;
using Castle.DynamicProxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CrossCutting.Core.Configuration.ConfigObjects;

public class ConfigObjectProvider : IConfigObjectProvider
{
    private readonly IConfigurator _configurator;
    private readonly IDictionary<Type, object> _configObjects;
    private readonly ProxyGenerator _proxyGenerator;

    public ConfigObjectProvider(IConfigurator configurator)
    {
        if (configurator == null) throw new ArgumentNullException(nameof(configurator));

        _configObjects = new ConcurrentDictionary<Type, object>();
        _configurator = configurator;
        _proxyGenerator = new ProxyGenerator();
    }

    public TConfig Get<TConfig>()
    {
        Type configType = typeof(TConfig);

        ValidateType(configType);

        object configObj = GetConfigObject(configType);
        return (TConfig)configObj;
    }

    public object Get(Type configType)
    {
        if (configType == null) throw new ArgumentNullException(nameof(configType));

        ValidateType(configType);

        object configObj = GetConfigObject(configType);
        return configObj;
    }

    private void ValidateType(Type type)
    {
        PropertyInfo[] properties = type.GetProperties();

        bool allAreVirtual = properties.All(p => p.GetMethod.IsVirtual);
        bool allHaveAttributes = properties.All(p => p.GetCustomAttributes<ConfigMapAttribute>().Any());

        if (!allHaveAttributes || !allAreVirtual)
        {
            throw new InvalidOperationException("Requested type can only consist of virtual properties with ConfigMapAttribute");
        }
    }

    private object GetConfigObject(Type configType)
    {
        bool alreadyGenerated = _configObjects.ContainsKey(configType);
        if (alreadyGenerated)
        {
            object obj = _configObjects[configType];
            return obj;
        }
        else
        {
            object obj = _proxyGenerator.CreateClassProxy(configType, ProxyGenerationOptions.Default,
                new ConfigObjectInterceptor(_configurator));

            _configObjects[configType] = obj;

            return obj;
        }
    }
}