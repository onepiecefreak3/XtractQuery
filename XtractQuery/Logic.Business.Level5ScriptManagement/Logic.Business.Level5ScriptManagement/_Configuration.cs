using CrossCutting.Core.Contract.Configuration.DataClasses;

namespace Logic.Business.Level5ScriptManagement;

public class ScriptManagementConfiguration
{
    [ConfigMap("CommandLine", ["h", "help"])]
    public virtual bool ShowHelp { get; set; }

    [ConfigMap("CommandLine", ["o", "operation"])]
    public virtual string Operation { get; set; }

    [ConfigMap("CommandLine", ["t", "type"])]
    public virtual string QueryType { get; set; }

    [ConfigMap("CommandLine", ["l", "length"])]
    public virtual string Length { get; set; } = "int";

    [ConfigMap("CommandLine", ["nc", "no-compression"])]
    public virtual bool WithoutCompression { get; set; } = false;

    [ConfigMap("CommandLine", ["f", "file"])]
    public virtual string InputPath { get; set; }

    [ConfigMap("Logic.Business.Level5ScriptManagement", "MethodMappingPath")]
    public virtual string MethodMappingPath { get; set; } = "methodMapping.json";

    [ConfigMap("Logic.Business.Level5ScriptManagement", "ReferenceScriptPath")]
    public virtual string ReferenceScriptPath { get; set; } = "reference";
}