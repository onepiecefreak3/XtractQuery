using CrossCutting.Core.Contract.Configuration.DataClasses;

namespace Logic.Business.Level5ScriptManagement
{
    public class Level5ScriptManagementConfiguration
    {
        [ConfigMap("CommandLine", new[] { "h", "help" })]
        public virtual bool ShowHelp { get; set; }

        [ConfigMap("CommandLine", new[] { "o", "operation" })]
        public virtual string Operation { get; set; }

        [ConfigMap("CommandLine", new[] { "t", "type" })]
        public virtual string QueryType { get; set; }

        [ConfigMap("CommandLine", new[] { "l", "length" })]
        public virtual string Length { get; set; } = "int";

        [ConfigMap("CommandLine", new[] { "nc", "no-compression" })]
        public virtual bool WithoutCompression { get; set; } = false;

        [ConfigMap("CommandLine", new[] { "f", "file" })]
        public virtual string FilePath { get; set; }

        [ConfigMap("Logic.Business.Level5ScriptManagement", "MethodMappingPath")]
        public virtual string MethodMappingPath { get; set; } = "methodMapping.json";

        [ConfigMap("Logic.Business.Level5ScriptManagement", "ReferenceScriptPath")]
        public virtual string ReferenceScriptPath { get; set; } = "reference";
    }
}