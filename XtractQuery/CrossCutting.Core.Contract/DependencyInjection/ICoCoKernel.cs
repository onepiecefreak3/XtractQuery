using System;
using System.Diagnostics.CodeAnalysis;
using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using Microsoft.Extensions.DependencyInjection;

namespace CrossCutting.Core.Contract.DependencyInjection;

public interface ICoCoKernel
{
    void Build(string scopeName);

    void Register<TContract, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
        where TContract : class
        where TImplementation : class, TContract;
    void Register<TContract, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(ActivationScope scope)
        where TContract : class
        where TImplementation : class, TContract;

    void Register<TContract, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(string key)
        where TContract : class
        where TImplementation : class, TContract;
    void Register<TContract, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(string key, ActivationScope scope)
        where TContract : class
        where TImplementation : class, TContract;

    [RequiresUnreferencedCode("Open type registration requires runtime type inspection.")]
    void Register(Type contract, Type implementation);
    [RequiresUnreferencedCode("Open type registration requires runtime type inspection.")]
    void Register(Type contract, Type implementation, ActivationScope scope);

    [RequiresUnreferencedCode("Open type registration requires runtime type inspection.")]
    void Register(string key, Type contract, Type implementation);
    [RequiresUnreferencedCode("Open type registration requires runtime type inspection.")]
    void Register(string key, Type contract, Type implementation, ActivationScope scope);

    void RegisterInstance<TComponent>(TComponent instance)
        where TComponent : class;

    void RegisterInstanceKeyed<TComponent>(TComponent instance, Type registerAs, string key)
        where TComponent : class;

    void RegisterToSelf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>()
        where TImplementation : class;
    void RegisterToSelf<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(ActivationScope scope)
        where TImplementation : class;

    void RegisterComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TComponent>()
        where TComponent : class, IComponentActivator;

    TContract Get<TContract>()
        where TContract : class;
    [RequiresUnreferencedCode("ConstructorParameter resolution uses ActivatorUtilities which inspects constructors.")]
    TContract Get<TContract>(params ConstructorParameter[] parameters)
        where TContract : class;

    object Get(Type contractType);
    [RequiresUnreferencedCode("ConstructorParameter resolution uses ActivatorUtilities which inspects constructors.")]
    object Get(Type contractType, params ConstructorParameter[] parameters);

    void RegisterConfiguration<T>();

    IServiceProvider CreateServiceProvider();

    void Populate(IServiceCollection services, object lifetimeScopeTag);

    IScope CreateRequestScope();
}
