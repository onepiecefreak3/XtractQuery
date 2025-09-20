using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.Contract.EventBrokerage;
using Logic.Business.Level5ScriptManagement.Contract;
using Logic.Business.Level5ScriptManagement.Creation;
using Logic.Business.Level5ScriptManagement.Decompression;
using Logic.Business.Level5ScriptManagement.Extraction;
using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Business.Level5ScriptManagement.InternalContract.Creation;
using Logic.Business.Level5ScriptManagement.InternalContract.Decompression;
using Logic.Business.Level5ScriptManagement.InternalContract.Extraction;

namespace Logic.Business.Level5ScriptManagement;

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
        kernel.Register<IScriptManagementWorkflow, ScriptManagementWorkflow>(ActivationScope.Unique);

        kernel.Register<IExtractWorkflow, ExtractWorkflow>(ActivationScope.Unique);
        kernel.Register<IExtractXq32Workflow, ExtractXq32Workflow>(ActivationScope.Unique);
        kernel.Register<IExtractXseqWorkflow, ExtractXseqWorkflow>(ActivationScope.Unique);

        kernel.Register<ICreateWorkflow, CreateWorkflow>(ActivationScope.Unique);
        kernel.Register<ICreateXq32Workflow, CreateXq32Workflow>(ActivationScope.Unique);
        kernel.Register<ICreateXseqWorkflow, CreateXseqWorkflow>(ActivationScope.Unique);

        kernel.Register<IDecompressWorkflow, DecompressWorkflow>(ActivationScope.Unique);
        kernel.Register<IDecompressXq32Workflow, DecompressXq32Workflow>(ActivationScope.Unique);
        kernel.Register<IDecompressXseqWorkflow, DecompressXseqWorkflow>(ActivationScope.Unique);

        kernel.Register<ILevel5ScriptFileConverter, Level5ScriptFileConverter>();
        kernel.Register<ILevel5CodeUnitConverter, Level5CodeUnitConverter>();

        kernel.Register<IMethodNameMapper, MethodNameMapper>(ActivationScope.Unique);

        kernel.Register<IScriptManagementConfigurationValidator, ScriptManagementConfigurationValidator>(ActivationScope.Unique);

        kernel.RegisterConfiguration<ScriptManagementConfiguration>();
    }

    public void AddMessageSubscriptions(IEventBroker broker)
    {
    }

    public void Configure(IConfigurator configurator)
    {
    }
}