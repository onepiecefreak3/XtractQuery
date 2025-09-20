using System.Text;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

namespace Logic.Domain.Kuriimu2.KomponentAdapter;

internal class BinaryReaderX : IBinaryReaderX
{
    private readonly Komponent.IO.BinaryReaderX _reader;

    public Stream BaseStream => _reader.BaseStream;

    public BitOrder BitOrder
    {
        get => (BitOrder)_reader.BitOrder;
        set => _reader.BitOrder = (Kontract.Models.IO.BitOrder)value;
    }

    public BitOrder EffectiveBitOrder => (BitOrder)_reader.EffectiveBitOrder;

    public ByteOrder ByteOrder
    {
        get => (ByteOrder)_reader.ByteOrder;
        set => _reader.ByteOrder = (Kontract.Models.IO.ByteOrder)value;
    }

    public int BlockSize
    {
        get => _reader.BlockSize;
        set => _reader.BlockSize = value;
    }

    public BinaryReaderX(Stream input, Encoding encoding, bool leaveOpen, ByteOrder byteOrder, BitOrder bitOrder, int blockSize)
    {
        _reader = new Komponent.IO.BinaryReaderX(input, encoding, leaveOpen, (Kontract.Models.IO.ByteOrder)byteOrder, (Kontract.Models.IO.BitOrder)bitOrder, blockSize);
    }

    public void SeekAlignment(int alignment = 0x10)
    {
        _reader.SeekAlignment(alignment);
    }

    public bool ReadBoolean()
    {
        return _reader.ReadBoolean();
    }

    public byte ReadByte()
    {
        return _reader.ReadByte();
    }

    public sbyte ReadSByte()
    {
        return _reader.ReadSByte();
    }

    public char ReadChar()
    {
        return _reader.ReadChar();
    }

    public short ReadInt16()
    {
        return _reader.ReadInt16();
    }

    public ushort ReadUInt16()
    {
        return _reader.ReadUInt16();
    }

    public int ReadInt32()
    {
        return _reader.ReadInt32();
    }

    public uint ReadUInt32()
    {
        return _reader.ReadUInt32();
    }

    public long ReadInt64()
    {
        return _reader.ReadInt64();
    }

    public ulong ReadUInt64()
    {
        return _reader.ReadUInt64();
    }

    public float ReadSingle()
    {
        return _reader.ReadSingle();
    }

    public double ReadDouble()
    {
        return _reader.ReadDouble();
    }

    public decimal ReadDecimal()
    {
        return _reader.ReadDecimal();
    }

    public string ReadCStringSJIS()
    {
        return _reader.ReadCStringSJIS();
    }

    public string ReadString()
    {
        return _reader.ReadString();
    }

    public string ReadString(int length)
    {
        return _reader.ReadString(length);
    }

    public string ReadString(int length, Encoding encoding)
    {
        return _reader.ReadString(length, encoding);
    }

    public long ReadBits(int count)
    {
        return _reader.ReadBits<long>(count);
    }

    public void ResetBitBuffer()
    {
        _reader.ResetBitBuffer();
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}