using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Builder;
using Autofac.Core;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;

namespace CrossCutting.Core.DI.AutofacAdapter.InternalContract
{
    public interface IInstanceContext
    {
        IReadOnlyCollection<ILifetimeScope> Scopes { get; }

        void AddScope(ILifetimeScope scope);

        void SetScope<T>(IRegistrationBuilder<T, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration, ActivationScope scope);

        void ApplyInterceptors<TContract, TImplementation>(IRegistrationBuilder<TImplementation, ConcreteReflectionActivatorData, SingleRegistrationStyle> registration, Type contractType)
            where TImplementation : TContract;

        void OnPreparing(PreparingEventArgs e);
    }
}