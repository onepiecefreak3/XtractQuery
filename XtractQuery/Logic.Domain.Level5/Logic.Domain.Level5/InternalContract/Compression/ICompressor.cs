using Logic.Domain.Level5.Contract.Enums.Compression;

namespace Logic.Domain.Level5.InternalContract.Compression;

public interface ICompressor
{
    Stream Compress(Stream input, CompressionType compressionType);
}