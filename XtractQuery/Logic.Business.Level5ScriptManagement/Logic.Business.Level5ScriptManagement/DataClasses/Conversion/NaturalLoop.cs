namespace Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

/// <summary>
/// A natural loop recovered from a back-edge <c>latch → header</c> where the header
/// dominates the latch.
/// </summary>
internal class NaturalLoop
{
    public required StatementBlock Header { get; init; }

    public required IReadOnlyList<StatementBlock> Latches { get; init; }

    public required IReadOnlySet<StatementBlock> Body { get; init; }

    public required IReadOnlyList<ControlFlowEdge> BackEdges { get; init; }

    /// <summary>
    /// Unique successor of the header that lies outside the loop body, if any
    /// (the <c>while</c> exit target).
    /// </summary>
    public StatementBlock? LoopExit { get; init; }
}
