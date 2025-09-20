using System.Text;
using Kryptography.Hash.Crc;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum;

internal class JamCrcChecksum : IJamCrcChecksum
{
    private readonly Crc32 _crc;

    public JamCrcChecksum()
    {
        _crc = Crc32.JamCrc;
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

    public uint ComputeValue(string input)
    {
        return _crc.ComputeValue(input);
    }

    public uint ComputeValue(string input, Encoding enc)
    {
        return _crc.ComputeValue(input, enc);
    }

    public uint ComputeValue(Stream input)
    {
        return _crc.ComputeValue(input);
    }

    public uint ComputeValue(Span<byte> input)
    {
        return _crc.ComputeValue(input);
    }
}