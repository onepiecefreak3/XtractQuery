using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract;
using Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract.Exceptions;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract;

[MapException(typeof(Level5Lz10CompressionException))]
public interface ILevel5Lz10Compression : ICompression;