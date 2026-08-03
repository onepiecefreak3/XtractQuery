using CrossCutting.Core.Bootstrapping;
using CrossCutting.Core.Configuration;
using CrossCutting.Core.Configuration.CommandLine;
using CrossCutting.Core.Configuration.ConfigObjects;
using CrossCutting.Core.Configuration.File;
using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.Logging;
using CrossCutting.Core.DI.MsDependencyInjectionAdapter;
using CrossCutting.Core.EventBrokerage;
using CrossCutting.Core.Logging.SerilogAdapter;
using Logic.Business.Level5ScriptManagement;
using Logic.Domain.CodeAnalysis;
using Logic.Domain.Level5;

namespace Mappings.XtractQuery;

public class KernelInitializer : IKernelInitializer
{
    private IKernelContainer? _kernelContainer;

    public IKernelContainer CreateKernelContainer()
    {
        return _kernelContainer ??= new KernelContainer();
    }

    public void Initialize()
    {
        RegisterCoreComponents(_kernelContainer!.Kernel);
        ActivateComponents(_kernelContainer.Kernel);
    }

    private void RegisterCoreComponents(ICoCoKernel kernel)
    {
        kernel.Register<IBootstrapper, Bootstrapper>(ActivationScope.Unique);
        kernel.Register<IEventBroker, EventBroker>(ActivationScope.Unique);
        kernel.Register<IConfigurationRepository, FileConfigurationRepository>();
        kernel.Register<IConfigurationRepository, CommandLineConfigurationRepository>();
        kernel.Register<IConfigurator, Configurator>(ActivationScope.Unique);
        kernel.Register<IConfigObjectProvider, ConfigObjectProvider>(ActivationScope.Unique);
        kernel.RegisterConfiguration<LoggingSerilogConfiguration>();
        kernel.Register<ILogger, Logger>(ActivationScope.Unique);
    }

    private void ActivateComponents(ICoCoKernel kernel)
    {
        // Add own components
        kernel.RegisterComponent<Level5ScriptManagementActivator>();
        kernel.RegisterComponent<Level5Activator>();

        kernel.RegisterComponent<CodeAnalysisActivator>();
    }
}
