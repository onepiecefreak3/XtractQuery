using System.Buffers.Binary;
using Komponent.Streams;
using Kompression;
using Kompression.Contract;
using Logic.Domain.Level5.Contract.Enums.Compression;
using Logic.Domain.Level5.InternalContract.Compression;

namespace Logic.Domain.Level5.Compression;

internal class Decompressor : IDecompressor
{
    public Stream Decompress(Stream input, long offset)
    {
        ICompression compression;

        input.Position = offset;

        CompressionType method = PeekCompressionMethod(input, out int decompressedSize);

        MemoryStream ms = new();
        switch (method)
        {
            case CompressionType.None:
                Stream subStream = new SubStream(input, offset + 4, decompressedSize);
                subStream.CopyTo(ms);
                break;

            case CompressionType.Lz10:
                compression = Compressions.Level5.Lz10.Build();
                compression.Decompress(input, ms);
                break;

            case CompressionType.Huffman4Bit:
                compression = Compressions.Level5.Huffman4Bit.Build();
                compression.Decompress(input, ms);
                break;

            case CompressionType.Huffman8Bit:
                compression = Compressions.Level5.Huffman8Bit.Build();
                compression.Decompress(input, ms);
                break;

            case CompressionType.Rle:
                compression = Compressions.Level5.Rle.Build();
                compression.Decompress(input, ms);
                break;

            case CompressionType.ZLib:
                Stream zlibStream = new SubStream(input, offset + 4);

                compression = Compressions.ZLib.Build();
                compression.Decompress(zlibStream, ms);
                break;

            default:
                throw new InvalidOperationException($"Unknown compression method {method}.");
        }

        ms.Position = 0;
        return ms;
    }

    public CompressionType PeekCompressionType(Stream input, long offset)
    {
        long bkPos = input.Position;
        input.Position = offset;

        CompressionType type = PeekCompressionMethod(input, out _);

        input.Position = bkPos;
        return type;
    }

    private CompressionType PeekCompressionMethod(Stream input, out int decompressedSize)
    {
        var buffer = new byte[4];

        int _ = input.Read(buffer);
        input.Position -= buffer.Length;

        uint methodSize = BinaryPrimitives.ReadUInt32LittleEndian(buffer);

        decompressedSize = (int)(methodSize >> 3);
        return (CompressionType)(methodSize & 0x7);
    }
}