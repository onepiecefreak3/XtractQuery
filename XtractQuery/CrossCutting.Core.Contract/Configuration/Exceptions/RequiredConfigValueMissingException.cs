using System;

namespace CrossCutting.Core.Contract.Configuration.Exceptions;

public class RequiredConfigValueMissingException : ConfigurationException
{
    public string Category { get; }
    public string Key { get; }

    public RequiredConfigValueMissingException(string category, string key)
        : base($"Required configuration value missing for category '{category}' and key '{key}'.")
    {
        Category = category;
        Key = key;
    }

    public RequiredConfigValueMissingException(string category, string key, Exception inner)
        : base($"Required configuration value missing for category '{category}' and key '{key}'.", inner)
    {
        Category = category;
        Key = key;
    }
}
