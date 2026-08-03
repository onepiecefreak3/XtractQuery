using System.Text.Json;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.Configuration.DataClasses;
using CrossCutting.Core.Configuration.File.Documents;

namespace CrossCutting.Core.Configuration.File;

public class FileConfigurationRepository : IConfigurationRepository
{
    public IEnumerable<ConfigCategory> Load()
    {
        string cfgPath = GetConfigPath();
        if (!System.IO.File.Exists(cfgPath))
            yield break;

        string json = System.IO.File.ReadAllText(cfgPath);
        if (string.IsNullOrWhiteSpace(json))
            yield break;

        List<ConfigCategoryDocument>? documents =
            JsonSerializer.Deserialize(json, ConfigFileJsonContext.Default.ListConfigCategoryDocument);

        if (documents is null)
            yield break;

        foreach (ConfigCategoryDocument document in documents)
            yield return MapCategory(document);
    }

    private static ConfigCategory MapCategory(ConfigCategoryDocument document)
    {
        var category = new ConfigCategory { Name = document.Name };

        foreach (ConfigEntryDocument entry in document.Entries)
            category.AddEntry(entry.Key, ConvertValue(entry.Value));

        return category;
    }

    private static object? ConvertValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt64(out long l) => l,
            JsonValueKind.Number when element.TryGetDouble(out double d) => d,
            JsonValueKind.Array => ConvertArray(element),
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    private static object ConvertArray(JsonElement element)
    {
        var values = new List<string>();
        foreach (JsonElement item in element.EnumerateArray())
            values.Add(item.ValueKind == JsonValueKind.String ? item.GetString() ?? string.Empty : item.GetRawText());
        return values;
    }

    private static string GetConfigPath()
    {
        return Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "config.json");
    }
}
