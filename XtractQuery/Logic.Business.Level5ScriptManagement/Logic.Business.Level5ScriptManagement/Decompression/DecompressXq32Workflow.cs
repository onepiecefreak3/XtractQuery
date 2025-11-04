using Logic.Business.Level5ScriptManagement.InternalContract.Decompression;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.Enums.Compression;
using Logic.Domain.Level5.Contract.Script.Xq32;

namespace Logic.Business.Level5ScriptManagement.Decompression;

class DecompressXq32Workflow(
    IXq32ScriptDecompressor decompressor,
    IXq32ScriptCompressor compressor)
    : IDecompressXq32Workflow
{
    public void Decompress(Stream input, Stream output)
    {
        // Decompress script data
        ScriptContainer container = decompressor.Decompress(input);

        // Write script data
        compressor.Compress(container, output, CompressionType.None);
    }
}