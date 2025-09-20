using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract.Exceptions;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract;

[MapException(typeof(JamCrcChecksumException))]
public interface IJamCrcChecksum : IChecksum<uint>;