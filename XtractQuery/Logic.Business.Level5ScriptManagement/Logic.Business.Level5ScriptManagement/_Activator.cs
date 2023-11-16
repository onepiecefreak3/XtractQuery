using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Business.Level5ScriptManagement.Contract;
using Logic.Business.Level5ScriptManagement.InternalContract;

namespace Logic.Business.Level5ScriptManagement
{
    public class Level5ScriptManagementActivator : IComponentActivator
    {
        public void Activating()
        {
        }

        public void Activated()
        {
        }

        public void Deactivating()
        {
        }

        public void Deactivated()
        {
        }

        public void Register(ICoCoKernel kernel)
        {
            kernel.Register<ILevel5ScriptManagementWorkflow, Level5ScriptManagementWorkflow>(ActivationScope.Unique);
            kernel.Register<ILevel5ScriptManagementConfigurationValidator, Level5ScriptManagementConfigurationValidator>(ActivationScope.Unique);

            kernel.Register<ILevel5ScriptFileConverter, Level5ScriptFileConverter>();
            kernel.Register<ILevel5CodeUnitConverter, Level5CodeUnitConverter>();

            kernel.Register<IMethodNameMapper, MethodNameMapper>(ActivationScope.Unique);

            kernel.RegisterConfiguration<Level5ScriptManagementConfiguration>();
        }

        public void AddMessageSubscriptions(IEventBroker broker)
        {
        }

        public void Configure(IConfigurator configurator)
        {
        }
    }
}
