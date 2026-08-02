namespace Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

internal enum BranchRegionKind
{
    IfThen,
    IfElse
}

/// <summary>
/// A single-entry branch region headed by a conditional block. The header dominates
/// the then/else arms; control reconverges at <see cref="Join"/>.
/// </summary>
internal class BranchRegion
{
    public required StatementBlock Header { get; init; }

    public required StatementBlock ThenEntry { get; init; }

    public StatementBlock? ElseEntry { get; init; }

    public required StatementBlock Join { get; init; }

    public required BranchRegionKind Kind { get; init; }

    public required IReadOnlySet<StatementBlock> ThenBlocks { get; init; }

    public required IReadOnlySet<StatementBlock> ElseBlocks { get; init; }
}
