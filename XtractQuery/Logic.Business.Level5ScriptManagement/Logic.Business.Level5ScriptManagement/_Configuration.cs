using CrossCutting.Core.Contract.Configuration.DataClasses;

namespace Logic.Business.Level5ScriptManagement;

[ConfigurationCategory("Logic.Business.Level5ScriptManagement")]
public class ScriptManagementConfiguration
{
    [ConfigurationKey("CommandLine", ["h", "help"])]
    public bool ShowHelp { get; set; }

    [ConfigurationKey("CommandLine", ["o", "operation"])]
    public string? Operation { get; set; }

    [ConfigurationKey("CommandLine", ["t", "type"])]
    public string? QueryType { get; set; }

    [ConfigurationKey("CommandLine", ["ns", "no-syntax"])]
    public bool WithoutHighLevelSyntax { get; set; }

    [ConfigurationKey("CommandLine", ["l", "length"])]
    public string Length { get; set; } = "int";

    [ConfigurationKey("CommandLine", ["nc", "no-compression"])]
    public bool WithoutCompression { get; set; }

    [ConfigurationKey("CommandLine", ["e", "encoding"])]
    public string Encoding { get; set; } = "sjis";

    [ConfigurationKey("CommandLine", ["f", "file"])]
    public string? InputPath { get; set; }

    public string MethodMappingPath { get; set; } = "methodMapping.json";

    public string ReferenceScriptPath { get; set; } = "reference";
}
