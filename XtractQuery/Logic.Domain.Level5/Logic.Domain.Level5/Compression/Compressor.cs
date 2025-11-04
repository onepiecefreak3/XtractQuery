using System.Buffers.Binary;
using Kompression;
using Kompression.Contract;
using Logic.Domain.Level5.Contract.Enums.Compression;
using Logic.Domain.Level5.InternalContract.Compression;

namespace Logic.Domain.Level5.Compression;

internal class Compressor : ICompressor
{
    public Stream Compress(Stream input, CompressionType compressionType)
    {
        ICompression compression;

        MemoryStream ms = new();
        switch (compressionType)
        {
            case CompressionType.None:
                WriteCompressionMethod(ms, input, compressionType);

                input.CopyTo(ms);
                break;

            case CompressionType.Lz10:
                compression = Compressions.Level5.Lz10.Build();
                compression.Compress(input, ms);
                break;

            case CompressionType.Huffman4Bit:
                compression = Compressions.Level5.Huffman4Bit.Build();
                compression.Compress(input, ms);
                break;

            case CompressionType.Huffman8Bit:
                compression = Compressions.Level5.Huffman8Bit.Build();
                compression.Compress(input, ms);
                break;

            case CompressionType.Rle:
                compression = Compressions.Level5.Rle.Build();
                compression.Compress(input, ms);
                break;

            case CompressionType.ZLib:
                WriteCompressionMethod(ms, input, compressionType);

                compression = Compressions.ZLib.Build();
                compression.Compress(input, ms);
                break;
        }

        ms.Position = 0;
        return ms;
    }

    private void WriteCompressionMethod(Stream output, Stream input, CompressionType compressionType)
    {
        uint compressionMethod = (uint)(input.Length << 3) | (uint)compressionType;

        var buffer = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, compressionMethod);

        output.Write(buffer);
    }
}