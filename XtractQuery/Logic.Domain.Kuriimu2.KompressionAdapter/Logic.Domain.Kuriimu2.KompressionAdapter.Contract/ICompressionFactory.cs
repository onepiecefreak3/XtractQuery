using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract.Exceptions;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.Contract;

[MapException(typeof(CompressionFactoryException))]
public interface ICompressionFactory
{
    ICompression Create(CompressionType type);
}