using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.InternalContract.Checksum;
using Logic.Domain.Level5.Script.Xq32;
using Logic.Domain.Level5.Script.Xseq;

namespace Logic.Domain.Level5.Script;

internal class StringTableFactory(IChecksumFactory checksumFactory, IScriptStringEncodingProvider encodingProvider)
    : IStringTableFactory
{
    public IStringTable Create(Stream input, ScriptType type)
    {
        return type switch
        {
            ScriptType.Xq32 => new Xq32StringTable(input, checksumFactory, encodingProvider),
            ScriptType.Xseq => new XseqStringTable(input, checksumFactory, encodingProvider),
            _ => throw new InvalidOperationException($"Unknown script type {type}.")
        };
    }

    public IStringTable Create(ScriptType type)
    {
        return type switch
        {
            ScriptType.Xq32 => new Xq32StringTable(checksumFactory, encodingProvider),
            ScriptType.Xseq => new XseqStringTable(checksumFactory, encodingProvider),
            _ => throw new InvalidOperationException($"Unknown script type {type}.")
        };
    }
}
