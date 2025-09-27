using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xscr;

public interface IXscrScriptCompressor
{
    void Compress(XscrCompressionContainer container, Stream output);
    void Compress(XscrCompressionContainer container, Stream output, CompressionType compressionType);
}