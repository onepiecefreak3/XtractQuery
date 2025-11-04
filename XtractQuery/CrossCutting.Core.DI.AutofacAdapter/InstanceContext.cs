using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extras.DynamicProxy;
using CrossCutting.Core.Contract.Aspects;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.DI.AutofacAdapter.Interception;
using CrossCutting.Core.DI.AutofacAdapter.InternalContract;

namespace CrossCutting.Core.DI.AutofacAdapter;

public class InstanceContext : IInstanceContext
{
    private static readonly Lazy<InstanceContext> s_lazyInstanceContext = new Lazy<InstanceContext>(() => new InstanceContext());

    private static readonly Dictionary<Type, Type> s_interceptorAttributeMap = new Dictionary<Type, Type>()
    {
        { typeof(MapExceptionAttribute), typeof(ExceptionMapInterceptor) }
    };

    private readonly Dictionary<Type, Func<Type, ResolvedParameter>> _resolvedParameterCache = new Dictionary<Type, Func<Type, ResolvedParameter>>();

    private readonly List<ILifetimeScope> _scopes;

    public static InstanceContext Instance
    {
        get
        {
            return s_lazyInstanceContext.Value;
        }
    }

    public IReadOnlyCollection<ILifetimeScope> Scopes
    {
        get
        {
            return _scopes.AsReadOnly();
        }
    }

    private InstanceContext()
    {
        _scopes = [];

        _resolvedParameterCache.Add(
            typeof(IScope),
            (t) => new ResolvedParameter(
                (p, c) => p.ParameterType == typeof(IScope),
                (p, c) => new Scope(_scopes.Last())));

        _resolvedParameterCache.Add(
            typeof(ILifetimeScope),
            (t) => new ResolvedParameter(
                (p, c) => p.ParameterType == typeof(ILifetimeScope),
                (p, c) => _scopes.Last()));
    }

    public void AddScope(ILifetimeScope scope)
    {
        _scopes.Add(scope);
    }

    public void SetScope<T>(IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> builder, ActivationScope scope)
    {
        switch (scope)
        {
            case ActivationScope.Request:
                builder.InstancePerLifetimeScope();
                break;
            case ActivationScope.Unique:
                builder.SingleInstance();
                break;
            case ActivationScope.Dependency:
                builder.InstancePerDependency();
                break;
            default:
                builder.InstancePerDependency();
                break;
        }
    }

    public void ApplyInterceptors<TContract, TImplementation>(IRegistrationBuilder<TImplementation, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration, Type contractType)
        where TImplementation : TContract
    {
        List<Attribute> attributes = contractType
            .GetCustomAttributes()
            .Where(a => s_interceptorAttributeMap.ContainsKey(a.GetType())).ToList();

        if (!attributes.Any())
            return;

        registration.EnableInterfaceInterceptors();

        foreach (Attribute attribute in attributes)
        {
            Type attributeType = attribute.GetType();

            if (s_interceptorAttributeMap.ContainsKey(attributeType))
            {
                Type interceptorType = s_interceptorAttributeMap[attributeType];
                registration.InterceptedBy(interceptorType.Name);
            }
        }
    }

    public void OnPreparing(PreparingEventArgs e)
    {
        Type activatorType = e.Component.Activator.LimitType;
        ConstructorInfo[] constructors = activatorType.GetConstructors();

        List<ParameterInfo> constructorParams = constructors
            .SelectMany(c => c.GetParameters())
            .ToList();

        if (!constructorParams.Any())
            return;

        IEnumerable<ResolvedParameter> parameters = _resolvedParameterCache
            .Where(rpc => constructorParams.Any(p => p.ParameterType == rpc.Key))
            .Select(rpc => rpc.Value(activatorType));

        e.Parameters = e.Parameters.Union(parameters);
    }
}