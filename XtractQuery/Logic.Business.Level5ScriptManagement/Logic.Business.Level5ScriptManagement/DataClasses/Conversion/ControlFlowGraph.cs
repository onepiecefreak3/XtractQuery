namespace Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

class ControlFlowGraph
{
    public required StatementBlock Entry { get; init; }

    public required StatementBlock Exit { get; init; }

    public required IReadOnlyList<StatementBlock> Blocks { get; init; }

    public required IReadOnlyList<ControlFlowEdge> Edges { get; init; }

    public required IReadOnlyDictionary<int, StatementBlock> BlockByStatementIndex { get; init; }
}
