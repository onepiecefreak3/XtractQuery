using Komponent.Streams;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;

namespace Logic.Domain.Kuriimu2.KomponentAdapter;

internal class StreamFactory : IStreamFactory
{
    public Stream CreateSubStream(Stream baseStream, long offset)
    {
        return new SubStream(baseStream, offset, baseStream.Length - offset);
    }

    public Stream CreateSubStream(Stream baseStream, long offset, long length)
    {
        return new SubStream(baseStream, offset, length);
    }
}