using Logic.Business.Level5ScriptManagement.InternalContract;

namespace Logic.Business.Level5ScriptManagement;

internal class ScriptManagementConfigurationValidator : IScriptManagementConfigurationValidator
{
    public void Validate(ScriptManagementConfiguration config)
    {
        if (config.ShowHelp)
            return;

        ValidateOperation(config);
        ValidateFilePath(config);
        ValidatePointerLength(config);
        ValidateQueryType(config);
    }

    private void ValidateOperation(ScriptManagementConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.Operation))
            throw new InvalidOperationException("No operation mode was given. Specify an operation mode by using the -o argument.");

        if (config.Operation != "e" && config.Operation != "c" && config.Operation != "d")
            throw new InvalidOperationException($"The operation mode '{config.Operation}' is not valid. Use -h to see a list of valid operation modes.");
    }

    private void ValidateFilePath(ScriptManagementConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.InputPath))
            throw new InvalidOperationException("No file to process was specified. Specify a file by using the -f argument.");

        if (!File.Exists(config.InputPath) && !Directory.Exists(config.InputPath))
            throw new InvalidOperationException($"File or directory '{Path.GetFullPath(config.InputPath)}' was not found.");
    }

    private void ValidatePointerLength(ScriptManagementConfiguration config)
    {
        if (config.Operation != "c")
            return;

        if (string.IsNullOrWhiteSpace(config.Length))
            throw new InvalidOperationException("No pointer length was given. Specify a pointer length by using the -l argument.");

        if (config.Length != "int" && config.Length != "long")
            throw new InvalidOperationException($"The pointer length '{config.Length}' is not valid. Use -h to see a list of valid pointer lengths.");
    }

    private void ValidateQueryType(ScriptManagementConfiguration config)
    {
        if (config.Operation != "c")
            return;

        if (string.IsNullOrWhiteSpace(config.QueryType))
            throw new InvalidOperationException("No query type was given. Specify a query type by using the -t argument.");

        if (config.QueryType != "xq32" && config.QueryType != "xseq" && config.QueryType != "xscr" && config.QueryType != "gss1" && config.QueryType != "gsd1")
            throw new InvalidOperationException($"The query type '{config.QueryType}' is not valid. Use -h to see a list of valid query types.");
    }
}