using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract;

[MapException(typeof(BinaryTypeWriterException))]
public interface IBinaryTypeWriter
{
    void Write(object value, IBinaryWriterX writer);

    void WriteMany<T>(IEnumerable<T> list, IBinaryWriterX writer);
}