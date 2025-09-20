using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Script.Xq32.InternalContract;
using Logic.Domain.Level5.Script.Xseq.InternalContract;

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
        switch (type)
        {
            case ScriptType.Xq32:
                return _kernel.Get<IXq32StringTable>(new ConstructorParameter("stream", input));

            case ScriptType.Xseq:
                return _kernel.Get<IXseqStringTable>(new ConstructorParameter("stream", input));

            default:
                throw new InvalidOperationException($"Unknown script type {type}.");
        }
    }

    public IStringTable Create(ScriptType type)
    {
        switch (type)
        {
            case ScriptType.Xq32:
                return _kernel.Get<IXq32StringTable>();

            case ScriptType.Xseq:
                return _kernel.Get<IXseqStringTable>();

            default:
                throw new InvalidOperationException($"Unknown script type {type}.");
        }
    }
}