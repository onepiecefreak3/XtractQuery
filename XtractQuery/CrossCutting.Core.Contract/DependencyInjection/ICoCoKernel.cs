using System;
using CrossCutting.Core.Contract.Aspects;
using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.Contract.DependencyInjection.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace CrossCutting.Core.Contract.DependencyInjection
{
    [MapException(typeof(DependencyInjectionException))]
    public interface ICoCoKernel
    {
        void Build(string scopeName);

        void Register<TContract, TImplementation>()
            where TImplementation : TContract;
        void Register<TContract, TImplementation>(ActivationScope scope)
            where TImplementation : TContract;

        void Register<TContract, TImplementation>(string key)
            where TImplementation : TContract;
        void Register<TContract, TImplementation>(string key, ActivationScope scope)
            where TImplementation : TContract;

        void Register(Type contract, Type implementation);
        void Register(Type contract, Type implementation, ActivationScope scope);

        void Register(string key, Type contract, Type implementation);
        void Register(string key, Type contract, Type implementation, ActivationScope scope);

        void RegisterInstance<TComponent>(TComponent instance)
            where TComponent : class;

        void RegisterInstanceKeyed<TComponent>(TComponent instance, Type registerAs, string key)
            where TComponent : class;

        void RegisterToSelf<TImplementation>();
        void RegisterToSelf<TImplementation>(ActivationScope scope);

        void RegisterComponent<TComponent>()
            where TComponent : IComponentActivator;

        TContract Get<TContract>()
            where TContract : class;
        TContract Get<TContract>(params ConstructorParameter[] parameters)
            where TContract : class;

        object Get(Type contractType);
        object Get(Type contractType, params ConstructorParameter[] parameters);

        void RegisterConfiguration<T>();

        IServiceProvider CreateServiceProvider();

        void Populate(IServiceCollection services, object lifetimeScopeTag);

        IScope CreateRequestScope();
    }
}