using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract.Exceptions;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;

[MapException(typeof(CrcChecksumFactoryException))]
public interface ICrcChecksumFactory
{
    IChecksum<uint> CreateCrc32(Crc32Type type);
    IChecksum<ushort> CreateCrc16(Crc16Type type);
}