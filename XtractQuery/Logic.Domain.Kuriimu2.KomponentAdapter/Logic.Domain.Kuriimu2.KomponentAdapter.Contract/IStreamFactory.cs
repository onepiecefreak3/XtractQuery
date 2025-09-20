using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract;

[MapException(typeof(StreamFactoryException))]
public interface IStreamFactory
{
    Stream CreateSubStream(Stream baseStream, long offset);
    Stream CreateSubStream(Stream baseStream, long offset, long length);
}