using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script;

public interface IScriptCompressor
{
    void Compress(ScriptContainer container, Stream output, bool hasCompression);
    void Compress(ScriptContainer container, Stream output, CompressionType compressionType);
}