using Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract;
using Kontract.Kompression;
using Kompression.Implementations;

namespace Logic.Domain.Kuriimu2.KompressionAdapter;

internal class Level5Lz10Compression: ILevel5Lz10Compression
{
    private readonly ICompression _compression;

    public Level5Lz10Compression()
    {
        _compression = Compressions.Level5.Lz10.Build();
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