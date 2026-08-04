using Logic.Domain.Level5.Contract.Script;
using CrossCutting.Core.Contract.DependencyInjection;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.InternalContract.Script.Xq32;
using Logic.Domain.Level5.InternalContract.Script.Xseq;

namespace Logic.Domain.Level5.Script;

internal class ScriptEntrySizeProviderFactory(ICoCoKernel kernel) : IScriptEntrySizeProviderFactory
{
    public IScriptEntrySizeProvider Create(ScriptType type)
    {
        return type switch
        {
            ScriptType.Xq32 => kernel.Get<IXq32ScriptEntrySizeProvider>(),
            ScriptType.Xseq => kernel.Get<IXseqScriptEntrySizeProvider>(),
            _ => throw new InvalidOperationException($"Unknown script type {type}.")
        };
    }
}