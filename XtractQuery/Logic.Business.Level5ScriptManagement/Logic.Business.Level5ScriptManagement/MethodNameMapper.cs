using System.Text.Json;
using Logic.Business.Level5ScriptManagement.InternalContract;
using System.Text.Json.Serialization;

namespace Logic.Business.Level5ScriptManagement;

internal partial class MethodNameMapper : IMethodNameMapper
{
    private readonly Dictionary<int, string> _methodNameMapping;
    private readonly Dictionary<string, int> _instructionTypeMapping;

    public MethodNameMapper(ScriptManagementConfiguration config)
    {
        _methodNameMapping = InitializeMapping(config.MethodMappingPath);
        _instructionTypeMapping = _methodNameMapping.ToDictionary(x => x.Value, y => y.Key);
    }

    public bool MapsInstructionType(int instructionType)
    {
        return _methodNameMapping.ContainsKey(instructionType);
    }

    public bool MapsMethodName(string methodName)
    {
        return _instructionTypeMapping.ContainsKey(methodName);
    }

    public string GetMethodName(int instructionType)
    {
        if (!_methodNameMapping.TryGetValue(instructionType, out string? methodName))
            throw new InvalidOperationException($"Instruction type {instructionType} is not mapped.");

        return methodName;
    }

    public int GetInstructionType(string methodName)
    {
        if (!_instructionTypeMapping.TryGetValue(methodName, out int instructionType))
            throw new InvalidOperationException($"Method name {methodName} is not mapped.");

        return instructionType;
    }

    private Dictionary<int, string> InitializeMapping(string mappingPath)
    {
        mappingPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), mappingPath);
        if (!File.Exists(mappingPath))
            return new Dictionary<int, string>();

        string mappingJson = File.ReadAllText(mappingPath);
        return JsonSerializer.Deserialize(mappingJson, MethodMappingJsonContext.Default.DictionaryInt32String);
    }

    [JsonSerializable(typeof(Dictionary<int, string>))]
    [JsonSourceGenerationOptions(ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true)]
    internal partial class MethodMappingJsonContext : JsonSerializerContext;
}