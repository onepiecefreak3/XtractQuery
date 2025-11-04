using Kryptography.Checksum;
using Kryptography.Checksum.Crc;
using Logic.Domain.Level5.InternalContract.Checksum;

namespace Logic.Domain.Level5.Checksum;

internal class ChecksumFactory : IChecksumFactory
{
    public Checksum<uint> CreateCrc32()
    {
        return Crc32.Crc32B;
    }

    public Checksum<ushort> CreateCrc16()
    {
        return Crc16.X25;
    }
}