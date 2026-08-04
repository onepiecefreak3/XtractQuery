using System;

namespace CrossCutting.Core.Contract.Configuration.DataClasses;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConfigurationCategoryAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
