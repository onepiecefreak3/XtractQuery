namespace Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

public sealed class NamedParameterGlobalConflictWarning
{
    public required string MethodName { get; init; }

    public required string ParameterName { get; init; }

    public required int ParameterIndex { get; init; }
}
