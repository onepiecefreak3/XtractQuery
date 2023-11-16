using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.EventBrokerage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace CrossCutting.Core.Bootstrapping
{
    public sealed class Bootstrapper : IBootstrapper
    {
        private readonly List<IComponentActivator> _components;

        public Bootstrapper(IComponentActivator[] components)
        {
            _components = components.ToList();
        }

        public void ActivatingAll() => _components.ForEach(ca => ca.Activating());
        public void ActivatedAll() => _components.ForEach(ca => ca.Activated());
        public void DeactivatedAll() => _components.ForEach(ca => ca.Deactivated());
        public void DeactivatingAll() => _components.ForEach(ca => ca.Deactivating());
        public void RegisterAll(ICoCoKernel kernel) => _components.ForEach(ca => ca.Register(kernel));
        public void AddAllMessageSubscriptions(IEventBroker broker) => _components.ForEach(ca => ca.AddMessageSubscriptions(broker));
        public void ConfigureAll(IConfigurator config) => _components.ForEach(ca => ca.Configure(config));
    }
}
