using System;

namespace CrossCutting.Core.Contract.Configuration.DataClasses;

/// <summary>
/// Binds a configuration property to keys in a non-default category.
/// Only category <c>CommandLine</c> is supported.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurationKeyAttribute(string category, string[] keys) : Attribute
{
    public string Category { get; } = category;
    public string[] Keys { get; } = keys;

    public ConfigurationKeyAttribute(string category, string key)
        : this(category, [key])
    {
    }
}
