using System.Runtime.Loader;
using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.EventBrokerage;
using Mappings.XtractQuery;

namespace XtractQuery
{
    internal class KernelLoader
    {
        private ICoCoKernel _kernel;
        private IBootstrapper _bootstrapper;

        public KernelLoader()
        {
            AssemblyLoadContext.Default.Unloading += (a) =>
            {
                _bootstrapper?.DeactivatingAll();
                _bootstrapper?.DeactivatedAll();
            };
        }

        public ICoCoKernel Initialize()
        {
            KernelInitializer kernelInitializer = new KernelInitializer();
            IKernelContainer kernelContainer = kernelInitializer.CreateKernelContainer();
            _kernel = kernelContainer.Kernel;

            kernelInitializer.Initialize();
            _kernel.Build("Bootstrap");

            ActivateComponents();
            _kernel.Build("Components");

            return _kernel;
        }

        private void ActivateComponents()
        {
            IConfigurator configurator = _kernel.Get<IConfigurator>();

            _bootstrapper = _kernel.Get<IBootstrapper>();
            _bootstrapper.ConfigureAll(configurator);
            _bootstrapper.ActivatingAll();
            _bootstrapper.ActivatedAll();
            _bootstrapper.RegisterAll(_kernel);

            IEventBroker eventBroker = _kernel.Get<IEventBroker>();
            eventBroker.SetResolverCallback(t => _kernel.Get(t));
            _bootstrapper.AddAllMessageSubscriptions(eventBroker);
        }
    }
}