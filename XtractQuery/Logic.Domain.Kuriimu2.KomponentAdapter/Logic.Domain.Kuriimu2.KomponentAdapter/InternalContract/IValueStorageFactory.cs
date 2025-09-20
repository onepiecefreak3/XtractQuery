using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.Exceptions;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract;

[MapException(typeof(ValueStorageFactoryException))]
public interface IValueStorageFactory
{
    IValueStorage Create();
    IValueStorage CreateScoped(IDictionary<string, object> store, string scope);
}