using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract;

namespace Logic.Domain.Kuriimu2.KomponentAdapter;

internal class ValueStorageFactory : IValueStorageFactory
{
    private readonly ICoCoKernel _kernel;

    public ValueStorageFactory(ICoCoKernel kernel)
    {
        _kernel = kernel;
    }

    public IValueStorage Create()
    {
        return _kernel.Get<IValueStorage>();
    }

    public IValueStorage CreateScoped(IDictionary<string, object> store, string scope)
    {
        return _kernel.Get<IValueStorage>(
            new ConstructorParameter("store", store),
            new ConstructorParameter("scope", scope));
    }
}