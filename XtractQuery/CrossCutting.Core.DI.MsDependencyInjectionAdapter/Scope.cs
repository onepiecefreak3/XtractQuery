using System.Runtime.CompilerServices;
using CrossCutting.Core.Contract.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CrossCutting.Core.DI.MsDependencyInjectionAdapter;

internal sealed class Scope(IServiceScope scope) : IScope
{
    private readonly IServiceScope _scope = scope ?? throw new ArgumentNullException(nameof(scope));

    public event EventHandler<ResolveRequestEventArgs>? ResolveRequest;

    public TContract Get<TContract>()
        where TContract : class
    {
        return (TContract)Get(typeof(TContract));
    }

    public object Get(Type contractType)
    {
        return _scope.ServiceProvider.GetRequiredService(contractType);
    }

    public string GetHash()
    {
        return RuntimeHelpers.GetHashCode(_scope).ToString();
    }

    public void Dispose()
    {
        _scope.Dispose();
    }
}
