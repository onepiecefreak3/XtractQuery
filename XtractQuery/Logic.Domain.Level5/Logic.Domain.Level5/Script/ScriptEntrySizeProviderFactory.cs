using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using CrossCutting.Core.Contract.DependencyInjection;
using Logic.Domain.Level5.Script.Xq32.InternalContract;
using Logic.Domain.Level5.Script.Xseq.InternalContract;

namespace Logic.Domain.Level5.Script;

internal class ScriptEntrySizeProviderFactory : IScriptEntrySizeProviderFactory
{
    private readonly ICoCoKernel _kernel;

    public ScriptEntrySizeProviderFactory(ICoCoKernel kernel)
    {
        _kernel = kernel;
    }

    public IScriptEntrySizeProvider Create(ScriptType type)
    {
        switch (type)
        {
            case ScriptType.Xq32:
                return _kernel.Get<IXq32ScriptEntrySizeProvider>();

            case ScriptType.Xseq:
                return _kernel.Get<IXseqScriptEntrySizeProvider>();

            default:
                throw new InvalidOperationException($"Unknown script type {type}.");
        }
    }
}