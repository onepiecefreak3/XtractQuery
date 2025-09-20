using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;
using Logic.Domain.Level5.Cryptography.InternalContract.Exceptions;

namespace Logic.Domain.Level5.Cryptography.InternalContract;

[MapException(typeof(ChecksumFactoryException))]
public interface IChecksumFactory
{
    IChecksum<uint> CreateCrc32();
    IChecksum<ushort> CreateCrc16();
}