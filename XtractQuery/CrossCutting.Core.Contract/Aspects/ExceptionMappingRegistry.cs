using System;
using System.Collections.Concurrent;

namespace CrossCutting.Core.Contract.Aspects;

public static class ExceptionMappingRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<object, object>> Factories = new();

    public static void Register<TContract>(Func<TContract, TContract> factory)
        where TContract : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        Factories[typeof(TContract)] = inner => factory((TContract)inner)!;
    }

    public static bool HasMapping(Type contractType)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        return Factories.ContainsKey(contractType);
    }

    public static bool TryWrap(Type contractType, object inner, out object wrapped)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        ArgumentNullException.ThrowIfNull(inner);

        if (Factories.TryGetValue(contractType, out Func<object, object>? factory))
        {
            wrapped = factory(inner);
            return true;
        }

        wrapped = inner;
        return false;
    }

    public static TContract WrapIfMapped<TContract>(TContract inner)
        where TContract : class
    {
        ArgumentNullException.ThrowIfNull(inner);

        if (Factories.TryGetValue(typeof(TContract), out Func<object, object>? factory))
            return (TContract)factory(inner);

        return inner;
    }
}
