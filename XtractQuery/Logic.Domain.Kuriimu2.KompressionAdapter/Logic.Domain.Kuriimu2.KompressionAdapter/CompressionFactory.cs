using CrossCutting.Core.Contract.DependencyInjection;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract;

namespace Logic.Domain.Kuriimu2.KompressionAdapter;

internal class CompressionFactory : ICompressionFactory
{
    private readonly ICoCoKernel _kernel;

    public CompressionFactory(ICoCoKernel kernel)
    {
        _kernel = kernel;
    }

    public ICompression Create(CompressionType type)
    {
        switch (type)
        {
            case CompressionType.Level5_Lz10:
                return _kernel.Get<ILevel5Lz10Compression>();

            case CompressionType.Level5_Huffman4Bit:
                return _kernel.Get<ILevel5Huffman4BitCompression>();

            case CompressionType.Level5_Huffman8Bit:
                return _kernel.Get<ILevel5Huffman8BitCompression>();

            case CompressionType.Level5_Rle:
                return _kernel.Get<ILevel5RleCompression>();

            case CompressionType.ZLib:
                return _kernel.Get<IZLibCompression>();

            default:
                throw new InvalidOperationException($"Unknown compression type {type}.");
        }
    }
}