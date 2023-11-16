using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Contract.Compression.DataClasses;

namespace Logic.Domain.Level5.Compression
{
    internal class Decompressor : IDecompressor
    {
        private readonly ICompressionFactory _compressionFactory;
        private readonly IStreamFactory _streamFactory;

        public Decompressor(ICompressionFactory compressionFactory, IStreamFactory streamFactory)
        {
            _compressionFactory = compressionFactory;
            _streamFactory = streamFactory;
        }

        public Stream Decompress(Stream input, long offset)
        {
            ICompression compression;

            input.Position = offset;

            CompressionType method = PeekCompressionMethod(input, out int decompressedSize);

            MemoryStream ms = new();
            switch (method)
            {
                case CompressionType.None:
                    Stream subStream = _streamFactory.CreateSubStream(input, offset + 4, decompressedSize);
                    subStream.CopyTo(ms);
                    break;

                case CompressionType.Lz10:
                    compression = _compressionFactory.Create(Kuriimu2.KompressionAdapter.Contract.DataClasses.CompressionType.Level5_Lz10);
                    compression.Decompress(input, ms);
                    break;

                case CompressionType.Huffman4Bit:
                    compression = _compressionFactory.Create(Kuriimu2.KompressionAdapter.Contract.DataClasses.CompressionType.Level5_Huffman4Bit);
                    compression.Decompress(input, ms);
                    break;

                case CompressionType.Huffman8Bit:
                    compression = _compressionFactory.Create(Kuriimu2.KompressionAdapter.Contract.DataClasses.CompressionType.Level5_Huffman8Bit);
                    compression.Decompress(input, ms);
                    break;

                case CompressionType.Rle:
                    compression = _compressionFactory.Create(Kuriimu2.KompressionAdapter.Contract.DataClasses.CompressionType.Level5_Rle);
                    compression.Decompress(input, ms);
                    break;

                case CompressionType.ZLib:
                    Stream zlibStream = _streamFactory.CreateSubStream(input, offset + 4);

                    compression = _compressionFactory.Create(Kuriimu2.KompressionAdapter.Contract.DataClasses.CompressionType.ZLib);
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
}
