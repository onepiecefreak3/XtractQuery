using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;
using System.Text;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract;

[MapException(typeof(BinaryFactoryException))]
public interface IBinaryFactory
{
    IBinaryReaderX CreateReader(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian,
        BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4);

    IBinaryReaderX CreateReader(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian,
        BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4);

    IBinaryReaderX CreateReader(Stream input, Encoding encoding, bool leaveOpen,
        ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst,
        int blockSize = 4);

    IBinaryReaderX CreateReader(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian,
        BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4);

    IBinaryWriterX CreateWriter(Stream input, ByteOrder byteOrder = ByteOrder.LittleEndian,
        BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4);

    IBinaryWriterX CreateWriter(Stream input, Encoding encoding, ByteOrder byteOrder = ByteOrder.LittleEndian,
        BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4);

    IBinaryWriterX CreateWriter(Stream input, Encoding encoding, bool leaveOpen,
        ByteOrder byteOrder = ByteOrder.LittleEndian, BitOrder bitOrder = BitOrder.MostSignificantBitFirst,
        int blockSize = 4);

    IBinaryWriterX CreateWriter(Stream input, bool leaveOpen, ByteOrder byteOrder = ByteOrder.LittleEndian,
        BitOrder bitOrder = BitOrder.MostSignificantBitFirst, int blockSize = 4);
}