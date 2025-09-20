using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Compression.InternalContract.Exceptions;
using Logic.Domain.Level5.Contract.Compression.DataClasses;

namespace Logic.Domain.Level5.Compression.InternalContract;

[MapException(typeof(CompressorException))]
public interface ICompressor
{
    Stream Compress(Stream input, CompressionType compressionType);
}