using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

class ControlFlowGraph
{
    public required StatementBlock Entry { get; init; }

    public required StatementBlock Exit { get; init; }

    public required IReadOnlyList<StatementBlock> Blocks { get; init; }

    public required IReadOnlyList<ControlFlowEdge> Edges { get; init; }

    public required IReadOnlyDictionary<int, StatementBlock> BlockByStatementIndex { get; init; }

    /// <summary>Flat statement list this graph was built from.</summary>
    public required IReadOnlyList<StatementSyntax> Statements { get; init; }

    /// <summary>Label name → basic block that defines it.</summary>
    public required IReadOnlyDictionary<string, StatementBlock> BlockByLabel { get; init; }

    /// <summary>Label name → statement index of the label definition.</summary>
    public required IReadOnlyDictionary<string, int> LabelStatementIndex { get; init; }

    /// <summary>Outgoing edges keyed by source block (includes exit).</summary>
    public required IReadOnlyDictionary<StatementBlock, IReadOnlyList<ControlFlowEdge>> OutgoingEdges { get; init; }

    /// <summary>Incoming edges keyed by target block (includes exit).</summary>
    public required IReadOnlyDictionary<StatementBlock, IReadOnlyList<ControlFlowEdge>> IncomingEdges { get; init; }
}
