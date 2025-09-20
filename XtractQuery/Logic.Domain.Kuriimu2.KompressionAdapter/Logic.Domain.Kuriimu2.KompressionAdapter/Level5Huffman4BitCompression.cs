using Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract;
using Kontract.Kompression;
using Kompression.Implementations;

namespace Logic.Domain.Kuriimu2.KompressionAdapter;

internal class Level5Huffman4BitCompression : ILevel5Huffman4BitCompression
{
    private readonly ICompression _compression;

    public Level5Huffman4BitCompression()
    {
        _compression = Compressions.Level5.Huffman4Bit.Build();
    }

    public void Decompress(Stream input, Stream output)
    {
        _compression.Decompress(input, output);
    }

    public void Compress(Stream input, Stream output)
    {
        _compression.Compress(input, output);
    }
}