using System;

namespace CrossCutting.Core.Contract.Bootstrapping.Exceptions;

public class BootstrappingException : Exception
{
    public BootstrappingException()
    {
    }

    public BootstrappingException(string message) : base(message)
    {
    }

    public BootstrappingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}