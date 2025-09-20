using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Extensions.DependencyInjection;
using Castle.DynamicProxy;
using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.DI.AutofacAdapter.Interception;
using CrossCutting.Core.DI.AutofacAdapter.InternalContract;
using Microsoft.Extensions.DependencyInjection;

namespace CrossCutting.Core.DI.AutofacAdapter;

public sealed class KernelAdapter : ICoCoKernel
{
    private readonly IInstanceContext _instanceContext;
    private ContainerBuilder _containerBuilder;

    public ContainerBuilder Builder
    {
        get
        {
            return _containerBuilder;
        }
    }

    public IReadOnlyCollection<ILifetimeScope> Scopes
    {
        get
        {
            return _instanceContext.Scopes;
        }
    }

    public KernelAdapter(ContainerBuilder containerBuilder)
    {
        _containerBuilder = containerBuilder;
        _instanceContext = InstanceContext.Instance;

        RegisterInterceptors();
    }

    private void RegisterInterceptors()
    {
        _containerBuilder
            .RegisterType<ExceptionMapInterceptor>()
            .UsingConstructor()
            .Named<IInterceptor>(nameof(ExceptionMapInterceptor));
    }

    public void Register<TContract, TImplementation>()
        where TImplementation : TContract
    {
        Register<TContract, TImplementation>(ActivationScope.Dependency);
    }

    public void Register<TContract, TImplementation>(ActivationScope scope)
        where TImplementation : TContract
    {
        Register<TContract, TImplementation>(null, scope);
    }

    public void Register<TContract, TImplementation>(string key)
        where TImplementation : TContract
    {
        Register<TContract, TImplementation>(key, ActivationScope.Dependency);
    }

    public void Register<TContract, TImplementation>(string key, ActivationScope scope)
        where TImplementation : TContract
    {
        Register(key, typeof(TContract), typeof(TImplementation), scope);
    }

    public void Register(Type contract, Type implementation)
    {
        Register(contract, implementation, ActivationScope.Dependency);
    }

    public void Register(Type contract, Type implementation, ActivationScope scope)
    {
        Register(null, contract, implementation, scope);
    }

    public void Register(string key, Type contract, Type implementation)
    {
        Register(key, contract, implementation, ActivationScope.Dependency);
    }

    public void Register(string key, Type contract, Type implementation, ActivationScope scope)
    {
        IRegistrationBuilder<object, ConcreteReflectionActivatorData, SingleRegistrationStyle> builder;

        builder = _containerBuilder
            .RegisterType(implementation)
            .As(contract)
            .OnPreparing(_instanceContext.OnPreparing);

        if (!string.IsNullOrWhiteSpace(key))
        {
            builder.Named(key, contract);
        }

        _instanceContext.SetScope(builder, scope);
        _instanceContext.ApplyInterceptors<object, object>(builder, contract);
    }

    public void RegisterToSelf<TImplementation>()
    {
        RegisterToSelf<TImplementation>(ActivationScope.Dependency);
    }

    public void RegisterToSelf<TImplementation>(ActivationScope scope)
    {
        IRegistrationBuilder<TImplementation, ConcreteReflectionActivatorData, SingleRegistrationStyle> builder;

        builder = _containerBuilder
            .RegisterType<TImplementation>()
            .AsSelf()
            .OnPreparing(_instanceContext.OnPreparing);

        _instanceContext.SetScope(builder, scope);
    }

    public void RegisterComponent<TComponent>()
        where TComponent : IComponentActivator
    {
        _containerBuilder
            .RegisterType<TComponent>()
            .As<IComponentActivator>();
    }

    public void RegisterInstance<TComponent>(TComponent instance)
        where TComponent : class
    {
        _containerBuilder
            .RegisterInstance(instance);
    }

    public void RegisterInstanceKeyed<TComponent>(TComponent instance, Type registerAs, string key)
        where TComponent : class
    {
        _containerBuilder
            .RegisterInstance(instance)
            .As(registerAs)
            .Keyed(key, registerAs);
    }

    public TContract Get<TContract>()
        where TContract : class
    {
        return Get(typeof(TContract)) as TContract;
    }

    public TContract Get<TContract>(params ConstructorParameter[] parameters)
        where TContract : class
    {
        return Get(typeof(TContract), parameters) as TContract;
    }

    public object Get(Type contractType)
    {
        return Scopes
            .LastOrDefault()?
            .Resolve(contractType);
    }

    public object Get(Type contractType, params ConstructorParameter[] parameters)
    {
        return Scopes
            .LastOrDefault()?
            .Resolve(contractType, parameters.Select(p => new NamedParameter(p.Name, p.Value)));
    }

    public void RegisterConfiguration<T>()
    {
        _containerBuilder
            .Register(c => c.Resolve<IConfigObjectProvider>().Get<T>());
    }

    public void Build(string scopeName)
    {
        ILifetimeScope lastScope = Scopes.LastOrDefault();
        ILifetimeScope scope;

        if (lastScope == null)
        {
            IContainer container = _containerBuilder.Build();
            scope = container.BeginLifetimeScope(scopeName);

            _containerBuilder = new ContainerBuilder();
        }
        else
        {
            scope = lastScope.BeginLifetimeScope(scopeName,
                builder => ApplyCallbacks(GetCallbackReferenceList(_containerBuilder), builder));
        }

        _instanceContext.AddScope(scope);
    }

    public IServiceProvider CreateServiceProvider()
    {
        return new AutofacServiceProvider(Scopes.Last());
    }

    public void Populate(IServiceCollection services, object lifetimeScopeTag)
    {
        Builder.Populate(services, lifetimeScopeTag);
    }

    public IScope CreateRequestScope()
    {
        ILifetimeScope lastScope = Scopes.LastOrDefault();
        if (lastScope == null)
            throw new NullReferenceException("No base scope for request available.");

        return new Scope(lastScope.BeginLifetimeScope(Autofac.Core.Lifetime.MatchingScopeLifetimeTags.RequestLifetimeScopeTag));
    }

    #region CallbackReflection

    private List<DeferredCallback> GetCallbackReferenceList(ContainerBuilder builder)
    {
        return ((List<DeferredCallback>)GetConfigurationCallbacksFieldInfo().GetValue(builder)).ToList();
    }

    private void ApplyCallbacks(List<DeferredCallback> callbacks, ContainerBuilder builder)
    {
        List<DeferredCallback> references = (List<DeferredCallback>)GetConfigurationCallbacksFieldInfo().GetValue(builder);
        references.AddRange(callbacks);
    }

    private FieldInfo GetConfigurationCallbacksFieldInfo()
    {
        return typeof(ContainerBuilder).GetField("_configurationCallbacks", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    #endregion
}