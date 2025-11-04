using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;
using System.Text;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract;

[MapException(typeof(BinaryReaderXException))]
public interface IBinaryReaderX : IDisposable
{
    Stream BaseStream { get; }

    BitOrder BitOrder { get; set; }
    ByteOrder ByteOrder { get; set; }

    int BlockSize { get; set; }

    void SeekAlignment(int alignment = 0x10);

    bool ReadBoolean();
    byte ReadByte();
    sbyte ReadSByte();
    char ReadChar();
    short ReadInt16();
    ushort ReadUInt16();
    int ReadInt32();
    uint ReadUInt32();
    long ReadInt64();
    ulong ReadUInt64();
    float ReadSingle();
    double ReadDouble();
    decimal ReadDecimal();

    string ReadString();
    string ReadString(int length);
    string ReadString(int length, Encoding encoding);

    long ReadBits(int count);
}