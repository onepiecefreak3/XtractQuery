using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Contract.Compression.DataClasses;

namespace Logic.Domain.Level5.Compression
{
    internal class Compressor : ICompressor
    {
        private readonly ICompressionFactory _compressionFactory;

        public Compressor(ICompressionFactory compressionFactory)
        {
            _compressionFactory = compressionFactory;
        }

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
                    compression = _compressionFactory.Create(Kuriimu2.KompressionAdapter.Contract.DataClasses.CompressionType.Level5_Lz10);
                    compression.Compress(input, ms);
                    break;

                case CompressionType.Huffman4Bit:
                    compression = _compressionFactory.Create(Kuriimu2.KompressionAdapter.Contract.DataClasses.CompressionType.Level5_Huffman4Bit);
                    compression.Compress(input, ms);
                    break;

                case CompressionType.Huffman8Bit:
                    compression = _compressionFactory.Create(Kuriimu2.KompressionAdapter.Contract.DataClasses.CompressionType.Level5_Huffman8Bit);
                    compression.Compress(input, ms);
                    break;

                case CompressionType.Rle:
                    compression = _compressionFactory.Create(Kuriimu2.KompressionAdapter.Contract.DataClasses.CompressionType.Level5_Rle);
                    compression.Compress(input, ms);
                    break;

                case CompressionType.ZLib:
                    WriteCompressionMethod(ms, input, compressionType);

                    compression = _compressionFactory.Create(Kuriimu2.KompressionAdapter.Contract.DataClasses.CompressionType.ZLib);
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
}
