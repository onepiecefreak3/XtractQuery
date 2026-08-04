using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.EventBrokerage;

namespace CrossCutting.Core.Contract.Bootstrapping;

public interface IBootstrapper
{
    void ActivatingAll();
    void ActivatedAll();
    void DeactivatedAll();
    void DeactivatingAll();
    void RegisterAll(ICoCoKernel kernel);
    void AddAllMessageSubscriptions(IEventBroker broker);
    void ConfigureAll(IConfigurator config);
}