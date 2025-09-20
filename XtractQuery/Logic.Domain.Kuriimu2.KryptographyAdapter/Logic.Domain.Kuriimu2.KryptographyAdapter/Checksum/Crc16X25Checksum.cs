using System.Text;
using Kryptography.Hash.Crc;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum;

internal class Crc16X25Checksum : ICrc16X25Checksum
{
    private readonly Crc16 _crc;

    public Crc16X25Checksum()
    {
        _crc = Crc16.X25;
    }

    public byte[] Compute(string input)
    {
        return _crc.Compute(input);
    }

    public byte[] Compute(string input, Encoding enc)
    {
        return _crc.Compute(input, enc);
    }

    public byte[] Compute(Stream input)
    {
        return _crc.Compute(input);
    }

    public byte[] Compute(Span<byte> input)
    {
        return _crc.Compute(input);
    }

    public ushort ComputeValue(string input)
    {
        return _crc.ComputeValue(input);
    }

    public ushort ComputeValue(string input, Encoding enc)
    {
        return _crc.ComputeValue(input, enc);
    }

    public ushort ComputeValue(Stream input)
    {
        return _crc.ComputeValue(input);
    }

    public ushort ComputeValue(Span<byte> input)
    {
        return _crc.ComputeValue(input);
    }
}