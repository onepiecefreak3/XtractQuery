using System.Diagnostics.CodeAnalysis;
using CrossCutting.Core.Contract.Aspects;
using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CrossCutting.Core.DI.MsDependencyInjectionAdapter;

public sealed class KernelAdapter : ICoCoKernel
{
    private IServiceCollection _services = new ServiceCollection();
    private ServiceProvider? _provider;
    private readonly Dictionary<Type, Type> _implementationMap = new();
    private readonly HashSet<Type> _singletonServiceTypes = new();

    public void Register<TContract, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
        where TContract : class
        where TImplementation : class, TContract
    {
        Register<TContract, TImplementation>(ActivationScope.Dependency);
    }

    public void Register<TContract, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(ActivationScope scope)
        where TContract : class
        where TImplementation : class, TContract
    {
        Register<TContract, TImplementation>(null, scope);
    }

    public void Register<TContract, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(string key)
        where TContract : class
        where TImplementation : class, TContract
    {
        Register<TContract, TImplementation>(key, ActivationScope.Dependency);
    }

    public void Register<TContract, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(string? key, ActivationScope scope)
        where TContract : class
        where TImplementation : class, TContract
    {
        ServiceLifetime lifetime = ToLifetime(scope);

        if (!string.IsNullOrWhiteSpace(key))
        {
            _services.Add(new ServiceDescriptor(typeof(TContract), key, typeof(TImplementation), lifetime));
            TrackRegistration(typeof(TContract), typeof(TImplementation), lifetime);
            return;
        }

        if (ExceptionMappingRegistry.HasMapping(typeof(TContract)) && typeof(TContract) != typeof(TImplementation))
        {
            _services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));
            _services.Add(new ServiceDescriptor(typeof(TContract), provider =>
            {
                TImplementation inner = provider.GetRequiredService<TImplementation>();
                return ExceptionMappingRegistry.WrapIfMapped<TContract>(inner);
            }, lifetime));
        }
        else
        {
            _services.Add(new ServiceDescriptor(typeof(TContract), typeof(TImplementation), lifetime));
        }

        TrackRegistration(typeof(TContract), typeof(TImplementation), lifetime);
    }

    [RequiresUnreferencedCode("Open type registration requires runtime type inspection.")]
    public void Register(Type contract, Type implementation)
    {
        Register(contract, implementation, ActivationScope.Dependency);
    }

    [RequiresUnreferencedCode("Open type registration requires runtime type inspection.")]
    public void Register(Type contract, Type implementation, ActivationScope scope)
    {
        Register(null, contract, implementation, scope);
    }

    [RequiresUnreferencedCode("Open type registration requires runtime type inspection.")]
    public void Register(string key, Type contract, Type implementation)
    {
        Register(key, contract, implementation, ActivationScope.Dependency);
    }

    [RequiresUnreferencedCode("Open type registration requires runtime type inspection.")]
    public void Register(string? key, Type contract, Type implementation, ActivationScope scope)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(implementation);

        ServiceLifetime lifetime = ToLifetime(scope);

        if (!string.IsNullOrWhiteSpace(key))
        {
            _services.Add(new ServiceDescriptor(contract, key, implementation, lifetime));
            TrackRegistration(contract, implementation, lifetime);
            return;
        }

        bool hasExceptionMapping = ExceptionMappingRegistry.HasMapping(contract);

        if (hasExceptionMapping && contract != implementation)
        {
            _services.Add(new ServiceDescriptor(implementation, implementation, lifetime));
            _services.Add(new ServiceDescriptor(contract, provider =>
            {
                object inner = provider.GetRequiredService(implementation);
                return ExceptionMappingRegistry.TryWrap(contract, inner, out object wrapped)
                    ? wrapped
                    : inner;
            }, lifetime));
        }
        else
        {
            _services.Add(new ServiceDescriptor(contract, implementation, lifetime));
        }

        TrackRegistration(contract, implementation, lifetime);
    }

    public void RegisterToSelf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
        where TImplementation : class
    {
        RegisterToSelf<TImplementation>(ActivationScope.Dependency);
    }

    public void RegisterToSelf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(ActivationScope scope)
        where TImplementation : class
    {
        ServiceLifetime lifetime = ToLifetime(scope);
        _services.Add(new ServiceDescriptor(typeof(TImplementation), typeof(TImplementation), lifetime));
        TrackRegistration(typeof(TImplementation), typeof(TImplementation), lifetime);
    }

    public void RegisterComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TComponent>()
        where TComponent : class, IComponentActivator
    {
        _services.Add(new ServiceDescriptor(typeof(IComponentActivator), typeof(TComponent), ServiceLifetime.Transient));
        TrackRegistration(typeof(IComponentActivator), typeof(TComponent), ServiceLifetime.Transient);
    }

    public void RegisterInstance<TComponent>(TComponent instance)
        where TComponent : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        _services.AddSingleton(instance);
        _singletonServiceTypes.Add(typeof(TComponent));
    }

    public void RegisterInstanceKeyed<TComponent>(TComponent instance, Type registerAs, string key)
        where TComponent : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(registerAs);
        _services.AddKeyedSingleton(registerAs, key, instance);
        _singletonServiceTypes.Add(registerAs);
    }

    public TContract Get<TContract>()
        where TContract : class
    {
        return (TContract)Get(typeof(TContract));
    }

    [RequiresUnreferencedCode("ConstructorParameter resolution uses ActivatorUtilities which inspects constructors.")]
    public TContract Get<TContract>(params ConstructorParameter[] parameters)
        where TContract : class
    {
        return (TContract)Get(typeof(TContract), parameters);
    }

    public object Get(Type contractType)
    {
        EnsureBuilt();
        return _provider!.GetRequiredService(contractType);
    }

    [RequiresUnreferencedCode("ConstructorParameter resolution uses ActivatorUtilities which inspects constructors.")]
    public object Get(Type contractType, params ConstructorParameter[] parameters)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        EnsureBuilt();

        if (parameters is null || parameters.Length == 0)
            return Get(contractType);

        if (!_implementationMap.TryGetValue(contractType, out Type? implementationType))
        {
            throw new InvalidOperationException(
                $"No implementation mapping registered for contract '{contractType.FullName}'.");
        }

        object[] extras = parameters.Select(p => p.Value).ToArray();
        object instance = ActivatorUtilities.CreateInstance(_provider!, implementationType, extras);

        return ExceptionMappingRegistry.TryWrap(contractType, instance, out object wrapped)
            ? wrapped
            : instance;
    }

    public void RegisterConfiguration<T>()
    {
        _services.AddTransient(typeof(T), provider =>
            provider.GetRequiredService<IConfigObjectProvider>().Get(typeof(T)));
    }

    public void Build(string scopeName)
    {
        if (_provider is not null)
        {
            FreezeResolvedSingletons();
            _provider.Dispose();
            _provider = null;
        }

        _provider = _services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });
    }

    public IServiceProvider CreateServiceProvider()
    {
        EnsureBuilt();
        return _provider!;
    }

    public void Populate(IServiceCollection services, object lifetimeScopeTag)
    {
        ArgumentNullException.ThrowIfNull(services);

        foreach (ServiceDescriptor descriptor in services)
            _services.Add(descriptor);
    }

    public IScope CreateRequestScope()
    {
        EnsureBuilt();
        return new Scope(_provider!.CreateScope());
    }

    private void TrackRegistration(Type contract, Type implementation, ServiceLifetime lifetime)
    {
        _implementationMap[contract] = implementation;

        if (lifetime == ServiceLifetime.Singleton)
        {
            _singletonServiceTypes.Add(contract);
            _singletonServiceTypes.Add(implementation);
        }
    }

    private void FreezeResolvedSingletons()
    {
        if (_provider is null)
            return;

        var frozen = new Dictionary<Type, object>();

        foreach (Type serviceType in _singletonServiceTypes.ToList())
        {
            object? instance = _provider.GetService(serviceType);
            if (instance is null)
                continue;

            frozen[serviceType] = instance;
        }

        if (frozen.Count == 0)
            return;

        var rebuilt = new ServiceCollection();
        var emitted = new HashSet<Type>();

        foreach (ServiceDescriptor descriptor in _services)
        {
            if (descriptor.Lifetime != ServiceLifetime.Singleton
                || !frozen.TryGetValue(descriptor.ServiceType, out object? instance))
            {
                rebuilt.Add(descriptor);
                continue;
            }

            if (!emitted.Add(descriptor.ServiceType))
                continue;

            if (descriptor.ServiceKey is not null)
                rebuilt.Add(ServiceDescriptor.KeyedSingleton(descriptor.ServiceType, descriptor.ServiceKey, instance));
            else
                rebuilt.Add(ServiceDescriptor.Singleton(descriptor.ServiceType, instance));
        }

        foreach ((Type serviceType, object instance) in frozen)
        {
            if (emitted.Add(serviceType))
                rebuilt.Add(ServiceDescriptor.Singleton(serviceType, instance));
        }

        _services = rebuilt;
    }

    private void EnsureBuilt()
    {
        if (_provider is null)
            throw new InvalidOperationException("Kernel has not been built. Call Build before resolving services.");
    }

    private static ServiceLifetime ToLifetime(ActivationScope scope)
    {
        return scope switch
        {
            ActivationScope.Unique => ServiceLifetime.Singleton,
            ActivationScope.Request => ServiceLifetime.Scoped,
            _ => ServiceLifetime.Transient
        };
    }
}
