using Kryptography.Checksum;

namespace Logic.Domain.Level5.InternalContract.Checksum;

public interface IChecksumFactory
{
    Checksum<uint> CreateCrc32();
    Checksum<ushort> CreateCrc16();
}