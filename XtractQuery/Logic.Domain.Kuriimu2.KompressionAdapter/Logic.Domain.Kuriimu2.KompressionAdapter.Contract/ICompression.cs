using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract.Exceptions;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.Contract;

[MapException(typeof(CompressionException))]
public interface ICompression
{
    void Decompress(Stream input, Stream output);
    void Compress(Stream input, Stream output);
}