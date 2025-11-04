using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.InternalContract.Script.Xq32;
using Logic.Domain.Level5.InternalContract.Script.Xseq;

namespace Logic.Domain.Level5.Script;

internal class StringTableFactory : IStringTableFactory
{
    private readonly ICoCoKernel _kernel;

    public StringTableFactory(ICoCoKernel kernel)
    {
        _kernel = kernel;
    }

    public IStringTable Create(Stream input, ScriptType type)
    {
        return type switch
        {
            ScriptType.Xq32 => _kernel.Get<IXq32StringTable>(new ConstructorParameter("stream", input)),
            ScriptType.Xseq => _kernel.Get<IXseqStringTable>(new ConstructorParameter("stream", input)),
            _ => throw new InvalidOperationException($"Unknown script type {type}.")
        };
    }

    public IStringTable Create(ScriptType type)
    {
        return type switch
        {
            ScriptType.Xq32 => _kernel.Get<IXq32StringTable>(),
            ScriptType.Xseq => _kernel.Get<IXseqStringTable>(),
            _ => throw new InvalidOperationException($"Unknown script type {type}.")
        };
    }
}