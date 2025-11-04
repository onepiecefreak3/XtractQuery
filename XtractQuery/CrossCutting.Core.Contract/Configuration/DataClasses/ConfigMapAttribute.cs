using System;

namespace CrossCutting.Core.Contract.Configuration.DataClasses;

[AttributeUsage(AttributeTargets.Property)]
public class ConfigMapAttribute : Attribute
{
    public string Category { get; }
    public string[] Keys { get; }

    public ConfigMapAttribute(string category, string key)
        : this(category, [key])
    {
    }

    public ConfigMapAttribute(string category, string[] keys)
    {
        Category = category;
        Keys = keys;
    }
}