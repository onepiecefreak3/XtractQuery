using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract;

[MapException(typeof(BinaryTypeReaderException))]
public interface IBinaryTypeReader
{
    T? Read<T>(IBinaryReaderX reader);
    object? Read(IBinaryReaderX reader, Type type);

    IList<T?> ReadMany<T>(IBinaryReaderX reader, int length);
    IList<object?> ReadMany(IBinaryReaderX reader, Type type, int length);
}