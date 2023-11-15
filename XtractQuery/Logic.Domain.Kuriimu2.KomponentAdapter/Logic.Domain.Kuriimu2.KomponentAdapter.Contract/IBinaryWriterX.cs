using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract
{
    [MapException(typeof(BinaryWriterXException))]
    public interface IBinaryWriterX : IDisposable
    {
        Stream BaseStream { get; }

        BitOrder BitOrder { get; set; }
        BitOrder EffectiveBitOrder { get; }
        ByteOrder ByteOrder { get; set; }

        int BlockSize { get; set; }

        void WriteAlignment(int alignment = 0x10, byte alignmentByte = 0);
        void WritePadding(int count, byte paddingByte = 0);

        void Write(ReadOnlySpan<byte> span);
        void Write(byte[] buffer);

        void Write(bool value);
        void Write(byte value);
        void Write(sbyte value);
        void Write(char value);
        void Write(short value);
        void Write(ushort value);
        void Write(int value);
        void Write(uint value);
        void Write(long value);
        void Write(ulong value);
        void Write(float value);
        void Write(double value);
        void Write(decimal value);
        void Write(string value);

        void WriteString(string value, Encoding encoding, bool leadingCount = true, bool nullTerminator = true);

        void WriteBits(long value, int bitCount);

        void Flush();
    }
}
