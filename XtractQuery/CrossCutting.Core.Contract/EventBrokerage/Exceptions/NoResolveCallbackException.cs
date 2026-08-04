using System;

namespace CrossCutting.Core.Contract.EventBrokerage.Exceptions;

[Serializable]
public class NoResolveCallbackException : EventBrokerageException
{
    public NoResolveCallbackException()
    {
    }

    public NoResolveCallbackException(string message) : base(message)
    {
    }

    public NoResolveCallbackException(string message, Exception inner) : base(message, inner)
    {
    }
}