using System;

namespace CrossCutting.Core.Contract.DependencyInjection
{
    public class ResolveRequestEventArgs
    {
        public Type Service { get; }

        public Type Target { get; }

        public IRequestContext RequestContext { get; }

        public ResolveRequestEventArgs(Type service, Type target, IRequestContext context)
        {
            Service = service;
            RequestContext = context;
            Target = target;
        }
    }
}