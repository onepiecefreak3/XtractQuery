using Logic.Business.Level5ScriptManagement.InternalContract.Decompression;
using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xscr;
using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Business.Level5ScriptManagement.Decompression;

class DecompressXscrWorkflow(
    IXscrScriptDecompressor decompressor,
    IXscrScriptCompressor compressor)
    : IDecompressXscrWorkflow
{
    public void Decompress(Stream input, Stream output)
    {
        // Decompress script data
        XscrScriptContainer container = decompressor.Decompress(input);

        // Write script data
        compressor.Compress(container, output, CompressionType.None);
    }
}