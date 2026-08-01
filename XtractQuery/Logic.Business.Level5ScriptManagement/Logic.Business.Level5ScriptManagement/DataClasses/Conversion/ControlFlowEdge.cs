namespace Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

class ControlFlowEdge
{
    public required StatementBlock Source { get; init; }

    public required StatementBlock Target { get; init; }

    public required ControlFlowEdgeKind Kind { get; init; }
}
