using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.InternalContract.Checksum;
using Logic.Domain.Level5.InternalContract.Script.Xq32;
using Logic.Domain.Level5.InternalContract.Script.Xseq;
using Logic.Domain.Level5.Script.Xq32;
using Logic.Domain.Level5.Script.Xseq;

namespace Logic.Domain.Level5.Script;

internal class StringTableFactory : IStringTableFactory
{
    private readonly IChecksumFactory _checksumFactory;
    private readonly IScriptStringEncodingProvider _encodingProvider;

    public StringTableFactory(IChecksumFactory checksumFactory, IScriptStringEncodingProvider encodingProvider)
    {
        _checksumFactory = checksumFactory;
        _encodingProvider = encodingProvider;
    }

    public IStringTable Create(Stream input, ScriptType type)
    {
        return type switch
        {
            ScriptType.Xq32 => new Xq32StringTable(input, _checksumFactory, _encodingProvider),
            ScriptType.Xseq => new XseqStringTable(input, _checksumFactory, _encodingProvider),
            _ => throw new InvalidOperationException($"Unknown script type {type}.")
        };
    }

    public IStringTable Create(ScriptType type)
    {
        return type switch
        {
            ScriptType.Xq32 => new Xq32StringTable(_checksumFactory, _encodingProvider),
            ScriptType.Xseq => new XseqStringTable(_checksumFactory, _encodingProvider),
            _ => throw new InvalidOperationException($"Unknown script type {type}.")
        };
    }
}
