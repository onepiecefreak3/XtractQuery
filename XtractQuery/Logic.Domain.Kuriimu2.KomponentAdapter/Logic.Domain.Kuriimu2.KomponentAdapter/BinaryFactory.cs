using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

namespace Logic.Domain.Kuriimu2.KomponentAdapter
{
    internal class BinaryFactory : IBinaryFactory
    {
        private readonly ICoCoKernel _kernel;

        public BinaryFactory(ICoCoKernel kernel)
        {
            _kernel = kernel;
        }

        public IBinaryReaderX CreateReader(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
        {
            return CreateReaderInternal(input, Encoding.UTF8, false, byteOrder, bitOrder, blockSize);
        }

        public IBinaryReaderX CreateReader(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
        {
            return CreateReaderInternal(input, encoding, false, byteOrder, bitOrder, blockSize);
        }

        public IBinaryReaderX CreateReader(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
        {
            return CreateReaderInternal(input, encoding, leaveOpen, byteOrder, bitOrder, blockSize);
        }

        public IBinaryReaderX CreateReader(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
        {
            return CreateReaderInternal(input, Encoding.UTF8, leaveOpen, byteOrder, bitOrder, blockSize);
        }

        public IBinaryWriterX CreateWriter(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
        {
            return CreateWriterInternal(input, Encoding.UTF8, false, byteOrder, bitOrder, blockSize);
        }

        public IBinaryWriterX CreateWriter(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
        {
            return CreateWriterInternal(input, encoding, false, byteOrder, bitOrder, blockSize);
        }

        public IBinaryWriterX CreateWriter(Stream input, Encoding encoding, bool leaveOpen,
            ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst,
            int blockSize = 4)
        {
            return CreateWriterInternal(input, encoding, leaveOpen, byteOrder, bitOrder, blockSize);
        }

        public IBinaryWriterX CreateWriter(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian,
            BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4)
        {
            return CreateWriterInternal(input, Encoding.UTF8, leaveOpen, byteOrder, bitOrder, blockSize);
        }

        private IBinaryReaderX CreateReaderInternal(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder, BitOrder bitOrder, int blockSize)
        {
            return _kernel.Get<IBinaryReaderX>(
                new ConstructorParameter("input", input),
                new ConstructorParameter("encoding", encoding),
                new ConstructorParameter("leaveOpen", leaveOpen),
                new ConstructorParameter("byteOrder", byteOrder),
                new ConstructorParameter("bitOrder", bitOrder),
                new ConstructorParameter("blockSize", blockSize));
        }

        private IBinaryWriterX CreateWriterInternal(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder, BitOrder bitOrder, int blockSize)
        {
            return _kernel.Get<IBinaryWriterX>(
                new ConstructorParameter("input", input),
                new ConstructorParameter("encoding", encoding),
                new ConstructorParameter("leaveOpen", leaveOpen),
                new ConstructorParameter("byteOrder", byteOrder),
                new ConstructorParameter("bitOrder", bitOrder),
                new ConstructorParameter("blockSize", blockSize));
        }
    }
}
