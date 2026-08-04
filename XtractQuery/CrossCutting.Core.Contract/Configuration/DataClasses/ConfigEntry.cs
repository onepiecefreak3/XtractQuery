using System.Runtime.Serialization;

namespace CrossCutting.Core.Contract.Configuration.DataClasses;

public class ConfigEntry(ConfigCategory category)
{
    [IgnoreDataMember]
    public ConfigCategory Category { get; set; } = category;

    public string? Key { get; set; }
    public object? Value { get; set; }
}