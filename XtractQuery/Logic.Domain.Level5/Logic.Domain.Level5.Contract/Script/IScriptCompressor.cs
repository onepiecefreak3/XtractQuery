using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.Enums.Compression;

namespace Logic.Domain.Level5.Contract.Script;

public interface IScriptCompressor
{
    void Compress(ScriptContainer container, Stream output, bool hasCompression);
    void Compress(ScriptContainer container, Stream output, CompressionType compressionType);
}