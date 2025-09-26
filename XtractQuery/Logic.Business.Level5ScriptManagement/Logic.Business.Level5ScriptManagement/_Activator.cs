using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.Contract.EventBrokerage;
using Logic.Business.Level5ScriptManagement.Contract;
using Logic.Business.Level5ScriptManagement.Conversion;
using Logic.Business.Level5ScriptManagement.Creation;
using Logic.Business.Level5ScriptManagement.Decompression;
using Logic.Business.Level5ScriptManagement.Extraction;
using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion;
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
        kernel.Register<IExtractGss1Workflow, ExtractGss1Workflow>(ActivationScope.Unique);
        kernel.Register<IExtractGsd1Workflow, ExtractGsd1Workflow>(ActivationScope.Unique);

        kernel.Register<ICreateWorkflow, CreateWorkflow>(ActivationScope.Unique);
        kernel.Register<ICreateXq32Workflow, CreateXq32Workflow>(ActivationScope.Unique);
        kernel.Register<ICreateXseqWorkflow, CreateXseqWorkflow>(ActivationScope.Unique);
        kernel.Register<ICreateGss1Workflow, CreateGss1Workflow>(ActivationScope.Unique);
        kernel.Register<ICreateGsd1Workflow, CreateGsd1Workflow>(ActivationScope.Unique);

        kernel.Register<IDecompressWorkflow, DecompressWorkflow>(ActivationScope.Unique);
        kernel.Register<IDecompressXq32Workflow, DecompressXq32Workflow>(ActivationScope.Unique);
        kernel.Register<IDecompressXseqWorkflow, DecompressXseqWorkflow>(ActivationScope.Unique);

        kernel.Register<IXq32ScriptFileConverter, Xq32ScriptFileConverter>(ActivationScope.Unique);
        kernel.Register<IXseqScriptFileConverter, XseqScriptFileConverter>(ActivationScope.Unique);
        kernel.Register<IGss1ScriptFileConverter, Gss1ScriptFileConverter>(ActivationScope.Unique);
        kernel.Register<IGsd1ScriptFileConverter, Gsd1ScriptFileConverter>(ActivationScope.Unique);

        kernel.Register<IXq32CodeUnitConverter, Xq32CodeUnitConverter>(ActivationScope.Unique);
        kernel.Register<IXseqCodeUnitConverter, XseqCodeUnitConverter>(ActivationScope.Unique);
        kernel.Register<IGss1CodeUnitConverter, Gss1CodeUnitConverter>(ActivationScope.Unique);
        kernel.Register<IGsd1CodeUnitConverter, Gsd1CodeUnitConverter>(ActivationScope.Unique);

        kernel.Register<IGss1CodeUnitReducer, Gss1CodeUnitReducer>(ActivationScope.Unique);

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