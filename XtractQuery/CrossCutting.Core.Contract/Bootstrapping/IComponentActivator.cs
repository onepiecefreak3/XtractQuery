using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.EventBrokerage;

namespace CrossCutting.Core.Contract.Bootstrapping
{
    public interface IComponentActivator
    {
        void Activating();
        void Activated();
        void Deactivating();
        void Deactivated();
        void Register(ICoCoKernel kernel);
        void AddMessageSubscriptions(IEventBroker broker);
        void Configure(IConfigurator configurator);
    }
}
