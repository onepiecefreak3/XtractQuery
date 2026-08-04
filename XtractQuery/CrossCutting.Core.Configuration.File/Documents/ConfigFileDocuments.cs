using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrossCutting.Core.Configuration.File.Documents;

internal sealed class ConfigCategoryDocument
{
    public string Name { get; set; } = string.Empty;
    public List<ConfigEntryDocument> Entries { get; set; } = [];
}

internal sealed class ConfigEntryDocument
{
    public string Key { get; set; } = string.Empty;
    public JsonElement Value { get; set; }
}

[JsonSerializable(typeof(List<ConfigCategoryDocument>))]
[JsonSerializable(typeof(ConfigCategoryDocument))]
[JsonSerializable(typeof(ConfigEntryDocument))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
internal partial class ConfigFileJsonContext : JsonSerializerContext;
