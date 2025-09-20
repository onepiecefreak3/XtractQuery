using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract;
using Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract.Exceptions;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract;

[MapException(typeof(Level5Huffman8BitCompressionException))]
public interface ILevel5Huffman8BitCompression : ICompression;