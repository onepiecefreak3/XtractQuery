using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;

/// <summary>
/// Helpers for contiguous <see cref="GotoLabelStatementSyntax"/> runs created when the
/// jump table sorts multiple labels that share one instruction index.
/// </summary>
internal static class ControlFlowLabelRuns
{
    public static int FindRunStart(IReadOnlyList<StatementSyntax> statements, int index)
    {
        int start = index;
        while (start > 0 && statements[start - 1] is GotoLabelStatementSyntax)
            start--;
        return start;
    }

    public static int FindRunEnd(IReadOnlyList<StatementSyntax> statements, int index)
    {
        int end = index;
        while (end < statements.Count && statements[end] is GotoLabelStatementSyntax)
            end++;
        return end;
    }

    public static HashSet<string> CollectLabels(
        IReadOnlyList<StatementSyntax> statements,
        int runStart,
        int runEnd)
    {
        var labels = new HashSet<string>(StringComparer.Ordinal);
        for (int i = runStart; i < runEnd; i++)
        {
            if (ControlFlowLabels.TryGetLabelDefinition(statements[i], out string? name) && name is not null)
                labels.Add(name);
        }

        return labels;
    }

    public static List<int> IndicesOfLabels(
        IReadOnlyList<StatementSyntax> statements,
        int runStart,
        int runEnd,
        Func<string, bool> predicate)
    {
        var indices = new List<int>();
        for (int i = runStart; i < runEnd; i++)
        {
            if (!ControlFlowLabels.TryGetLabelDefinition(statements[i], out string? name) || name is null)
                continue;
            if (predicate(name))
                indices.Add(i);
        }

        return indices;
    }
}
