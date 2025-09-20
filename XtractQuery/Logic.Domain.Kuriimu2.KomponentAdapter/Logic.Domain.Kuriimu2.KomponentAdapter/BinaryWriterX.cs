using System.Text;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

namespace Logic.Domain.Kuriimu2.KomponentAdapter;

internal class BinaryWriterX : IBinaryWriterX
{
    private readonly Komponent.IO.BinaryWriterX _writer;

    public Stream BaseStream => _writer.BaseStream;

    public BitOrder BitOrder
    {
        get => (BitOrder)_writer.BitOrder;
        set => _writer.BitOrder = (Kontract.Models.IO.BitOrder)value;
    }

    public BitOrder EffectiveBitOrder => (BitOrder)_writer.EffectiveBitOrder;

    public ByteOrder ByteOrder
    {
        get => (ByteOrder)_writer.ByteOrder;
        set => _writer.ByteOrder = (Kontract.Models.IO.ByteOrder)value;
    }

    public int BlockSize
    {
        get => _writer.BlockSize;
        set => _writer.BlockSize = value;
    }

    public BinaryWriterX(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder, BitOrder bitOrder, int blockSize)
    {
        _writer = new Komponent.IO.BinaryWriterX(input, encoding, leaveOpen, (Kontract.Models.IO.ByteOrder)byteOrder, (Kontract.Models.IO.BitOrder)bitOrder, blockSize);
    }

    public void WriteAlignment(int alignment = 16, byte alignmentByte = 0)
    {
        _writer.WriteAlignment(alignment, alignmentByte);
    }

    public void WritePadding(int count, byte paddingByte = 0)
    {
        _writer.WritePadding(count, paddingByte);
    }

    public void Write(ReadOnlySpan<byte> span)
    {
        _writer.Write(span);
    }

    public void Write(byte[] buffer)
    {
        _writer.Write(buffer);
    }

    public void Write(bool value)
    {
        _writer.Write(value);
    }

    public void Write(byte value)
    {
        _writer.Write(value);
    }

    public void Write(sbyte value)
    {
        _writer.Write(value);
    }

    public void Write(char value)
    {
        _writer.Write(value);
    }

    public void Write(short value)
    {
        _writer.Write(value);
    }

    public void Write(ushort value)
    {
        _writer.Write(value);
    }

    public void Write(int value)
    {
        _writer.Write(value);
    }

    public void Write(uint value)
    {
        _writer.Write(value);
    }

    public void Write(long value)
    {
        _writer.Write(value);
    }

    public void Write(ulong value)
    {
        _writer.Write(value);
    }

    public void Write(float value)
    {
        _writer.Write(value);
    }

    public void Write(double value)
    {
        _writer.Write(value);
    }

    public void Write(decimal value)
    {
        _writer.Write(value);
    }

    public void Write(string value)
    {
        _writer.Write(value);
    }

    public void WriteString(string value, Encoding encoding, bool leadingCount = true, bool nullTerminator = true)
    {
        _writer.WriteString(value, encoding, leadingCount, nullTerminator);
    }

    public void WriteBits(long value, int bitCount)
    {
        _writer.WriteBits(value, bitCount);
    }

    public void Flush()
    {
        _writer.Flush();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}