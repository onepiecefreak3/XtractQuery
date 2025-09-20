using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script;

public interface IScriptWriter
{
    void Write(ScriptFile script, Stream output, bool hasCompression);
    void Write(ScriptFile script, Stream output, CompressionType compressionType);
    void Write(ScriptContainer container, Stream output, bool hasCompression);
    void Write(ScriptContainer container, Stream output, CompressionType compressionType);
}