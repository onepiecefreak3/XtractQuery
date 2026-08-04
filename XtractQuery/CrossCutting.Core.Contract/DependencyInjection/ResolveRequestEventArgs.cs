using System;

namespace CrossCutting.Core.Contract.DependencyInjection;

public class ResolveRequestEventArgs(Type service, Type target, IRequestContext context)
{
    public Type Service { get; } = service;

    public Type Target { get; } = target;

    public IRequestContext RequestContext { get; } = context;
}