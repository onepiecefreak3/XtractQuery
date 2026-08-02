namespace Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

/// <summary>
/// Dominator tree plus recoverable SESE-style regions derived from a CFG.
/// </summary>
internal class ControlFlowRegions
{
    public required DominatorTree Dominators { get; init; }

    /// <summary>Natural loops, innermost (smallest body) first.</summary>
    public required IReadOnlyList<NaturalLoop> Loops { get; init; }

    /// <summary>Branch regions, innermost (smallest arms) first.</summary>
    public required IReadOnlyList<BranchRegion> Branches { get; init; }
}
