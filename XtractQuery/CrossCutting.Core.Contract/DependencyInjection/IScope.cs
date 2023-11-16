using System;

namespace CrossCutting.Core.Contract.DependencyInjection
{
    public interface IScope : IDisposable
    {
        TContract Get<TContract>()
            where TContract : class;

        object Get(Type contractType);

        string GetHash();

        event EventHandler<ResolveRequestEventArgs> ResolveRequest;
    }
}