using System;

namespace CrossCutting.Core.Contract.Configuration.DataClasses;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConfigurationCategoryAttribute : Attribute
{
    public string Name { get; }

    public ConfigurationCategoryAttribute(string name)
    {
        Name = name;
    }
}
