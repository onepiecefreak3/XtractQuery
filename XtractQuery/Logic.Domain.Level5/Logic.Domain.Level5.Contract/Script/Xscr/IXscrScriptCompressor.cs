using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;
using Logic.Domain.Level5.Contract.Enums.Compression;

namespace Logic.Domain.Level5.Contract.Script.Xscr;

public interface IXscrScriptCompressor
{
    void Compress(XscrCompressionContainer container, Stream output);
    void Compress(XscrCompressionContainer container, Stream output, CompressionType compressionType);
}