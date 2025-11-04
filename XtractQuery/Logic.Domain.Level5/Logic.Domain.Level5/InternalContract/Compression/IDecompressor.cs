using Logic.Domain.Level5.Contract.Enums.Compression;

namespace Logic.Domain.Level5.InternalContract.Compression;

public interface IDecompressor
{
    Stream Decompress(Stream input, long offset);
    CompressionType PeekCompressionType(Stream input, long offset);
}