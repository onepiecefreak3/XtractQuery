using System;

namespace CrossCutting.Core.Contract.Configuration.Exceptions;

[Serializable]
public class ConfigurationException : Exception
{
    public ConfigurationException()
    {
    }

    public ConfigurationException(string message) : base(message)
    {
    }

    public ConfigurationException(string message, Exception inner) : base(message, inner)
    {
    }
}