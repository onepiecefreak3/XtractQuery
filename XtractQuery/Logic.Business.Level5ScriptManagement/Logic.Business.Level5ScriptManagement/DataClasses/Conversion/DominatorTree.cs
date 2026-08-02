namespace Logic.Business.Level5ScriptManagement.DataClasses.Conversion;

/// <summary>
/// Dominator tree over CFG basic blocks. <c>Dominates(d, n)</c> means every path
/// from the entry to <c>n</c> goes through <c>d</c>.
/// </summary>
internal class DominatorTree
{
    public required StatementBlock Entry { get; init; }

    /// <summary>Immediate dominator of each block; entry maps to null.</summary>
    public required IReadOnlyDictionary<StatementBlock, StatementBlock?> ImmediateDominator { get; init; }

    /// <summary>Children in the dominator tree (blocks whose idom is the key).</summary>
    public required IReadOnlyDictionary<StatementBlock, IReadOnlyList<StatementBlock>> Children { get; init; }

    public bool Dominates(StatementBlock dominator, StatementBlock node)
    {
        if (ReferenceEquals(dominator, node))
            return true;

        StatementBlock? current = ImmediateDominator.GetValueOrDefault(node);
        while (current is not null)
        {
            if (ReferenceEquals(current, dominator))
                return true;

            current = ImmediateDominator.GetValueOrDefault(current);
        }

        return false;
    }

    public bool StrictlyDominates(StatementBlock dominator, StatementBlock node)
    {
        return !ReferenceEquals(dominator, node) && Dominates(dominator, node);
    }
}
