using Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;
using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Raises if / if-else from CFG branch shapes. Dominator branch regions prefer
/// innermost candidates; matching still uses local shape rules so raise quality
/// matches the pre-region pass.
/// </summary>
internal class StructuredIfPass(
    IControlFlowGraphBuilder cfgBuilder,
    IControlFlowRegionAnalyzer regionAnalyzer,
    ILevel5SyntaxFactory syntaxFactory) : IStructuredIfPass
{
    public IReadOnlyList<StatementSyntax> Apply(IReadOnlyList<StatementSyntax> statements)
    {
        return StructuredSyntaxRecursor.Apply(statements, ApplyFlat, syntaxFactory);
    }

    private IReadOnlyList<StatementSyntax> ApplyFlat(IReadOnlyList<StatementSyntax> statements)
    {
        var result = statements.ToList();
        var changed = true;

        while (changed)
        {
            changed = false;
            ControlFlowGraph cfg = cfgBuilder.Build(result);
            ControlFlowRegions regions = regionAnalyzer.Analyze(cfg);

            foreach (int headerIndex in CollectBranchHeaderCandidates(cfg, regions))
            {
                if (TryMatchIfElse(
                        cfg,
                        headerIndex,
                        out IfStatementSyntax? ifElse,
                        out int ifElseContentLength,
                        out IReadOnlyList<int>? extraRemovals,
                        out int ifElseJoinIndex,
                        out bool removeIfElseJoin) &&
                    ifElse is not null)
                {
                    // Highest indices first so earlier indices stay valid.
                    if (removeIfElseJoin)
                        result.RemoveAt(ifElseJoinIndex);

                    if (extraRemovals is not null)
                    {
                        foreach (int index in extraRemovals.OrderByDescending(i => i))
                        {
                            if (removeIfElseJoin && index == ifElseJoinIndex)
                                continue;
                            result.RemoveAt(index);
                        }
                    }

                    result.RemoveRange(headerIndex, ifElseContentLength);
                    result.Insert(headerIndex, ifElse);
                    changed = true;
                    break;
                }

                if (TryMatchIfThen(
                        cfg,
                        headerIndex,
                        out IfStatementSyntax? ifThen,
                        out int ifThenContentLength,
                        out int ifThenEndIndex,
                        out bool removeIfThenEnd) &&
                    ifThen is not null)
                {
                    if (removeIfThenEnd)
                        result.RemoveAt(ifThenEndIndex);

                    result.RemoveRange(headerIndex, ifThenContentLength);
                    result.Insert(headerIndex, ifThen);
                    changed = true;
                    break;
                }
            }
        }

        return result;
    }

    private static IEnumerable<int> CollectBranchHeaderCandidates(ControlFlowGraph cfg, ControlFlowRegions regions)
    {
        var seen = new HashSet<int>();

        // Smallest branch regions first (analyzer orders by ascending arm size).
        foreach (BranchRegion region in regions.Branches)
        {
            int index = region.Header.EndStatementIndex - 1;
            if (index >= 0 && seen.Add(index))
                yield return index;
        }

        foreach (StatementBlock block in cfg.Blocks)
        {
            if (block.IsExit || block.StatementCount <= 0)
                continue;

            int index = block.EndStatementIndex - 1;
            if (seen.Add(index))
                yield return index;
        }
    }

    private bool TryMatchIfElse(
        ControlFlowGraph cfg,
        int headerIndex,
        out IfStatementSyntax? replacement,
        out int contentLength,
        out IReadOnlyList<int>? extraRemovals,
        out int joinLabelIndex,
        out bool removeJoinLabel)
    {
        replacement = null;
        contentLength = 0;
        extraRemovals = null;
        joinLabelIndex = -1;
        removeJoinLabel = false;
        if (!cfg.BlockByStatementIndex.TryGetValue(headerIndex, out StatementBlock? headerBlock))
            return false;
        if (!ControlFlowGraphQueries.TryGetBranchSkip(
                cfg.Statements[headerIndex],
                syntaxFactory,
                out ExpressionSyntax? condition,
                out string? elseLabel) ||
            condition is null ||
            elseLabel is null)
            return false;
        if (!ControlFlowLabels.IsNumericJumpLabel(elseLabel))
            return false;
        if (!ControlFlowGraphQueries.TryGetLabelIndex(cfg, elseLabel, out int elseLabelIndex) ||
            elseLabelIndex <= headerIndex)
            return false;
        if (!ControlFlowGraphQueries.TryGetBlockByLabel(cfg, elseLabel, out StatementBlock? elseBlock) ||
            elseBlock is null)
            return false;
        // Header must branch to the else block and fall through into the then region.
        if (!ControlFlowGraphQueries.HasBranchTo(cfg, headerBlock, elseBlock) ||
            !ControlFlowGraphQueries.HasFallThrough(cfg, headerBlock))
            return false;
        if (ControlFlowLabels.CountLabelReferences(cfg.Statements, elseLabel) != 1)
            return false;

        // Empty else after jump-table hash sort: `goto JOIN; JOIN: ELSE:` or `ELSE: JOIN:`
        // with no else body — join goto sits just before a contiguous label run that
        // contains both ELSE and JOIN in either order.
        if (TryMatchEmptyElse(
                cfg,
                headerIndex,
                elseLabel,
                elseLabelIndex,
                condition,
                out replacement,
                out contentLength,
                out extraRemovals,
                out joinLabelIndex,
                out removeJoinLabel))
            return true;

        // Standard if/else: join goto immediately before ELSE (no co-located siblings).
        // Patterns with sibling labels in between are handled after loop raising (fixpoint).
        if (elseLabelIndex - 1 <= headerIndex)
            return false;
        if (cfg.Statements[elseLabelIndex - 1] is not GotoStatementSyntax joinGoto ||
            !ControlFlowLabels.TryGetSingleGotoTarget(joinGoto, out string? joinLabel) ||
            joinLabel is null)
            return false;
        if (!ControlFlowLabels.IsNumericJumpLabel(joinLabel))
            return false;
        if (!ControlFlowGraphQueries.TryGetLabelIndex(cfg, joinLabel, out joinLabelIndex) ||
            joinLabelIndex <= elseLabelIndex)
            return false;
        if (!ControlFlowGraphQueries.TryGetBlockByLabel(cfg, joinLabel, out StatementBlock? joinBlock) ||
            joinBlock is null)
            return false;
        // The join goto must target the join block in the CFG.
        if (!cfg.BlockByStatementIndex.TryGetValue(elseLabelIndex - 1, out StatementBlock? thenTailBlock) ||
            !ControlFlowGraphQueries.HasBranchTo(cfg, thenTailBlock, joinBlock))
            return false;
        // Nested if/else often shares a merge instruction with an outer join, so other
        // fallthrough labels may sit between the else body and this join (e.g. "@015@":
        // before "@021@":). Those labels are not part of the else body and must stay put.
        int elseContentStart = elseLabelIndex + 1;
        int elseContentEnd = ControlFlowGraphQueries.FindContentEndBeforeJoin(
            cfg.Statements, elseContentStart, joinLabelIndex);
        var thenBody = ControlFlowGraphQueries.Slice(cfg.Statements, headerIndex + 1, elseLabelIndex - 1);
        var elseBody = ControlFlowGraphQueries.Slice(cfg.Statements, elseContentStart, elseContentEnd);
        if (!ControlFlowGraphQueries.IsStructuredBody(thenBody) ||
            !ControlFlowGraphQueries.IsStructuredBody(elseBody))
            return false;
        if (ControlFlowLabels.ContainsLabelReference(thenBody, elseLabel) ||
            ControlFlowLabels.ContainsLabelReference(elseBody, elseLabel))
            return false;
        if (ControlFlowLabels.ContainsLabelReference(thenBody, joinLabel) ||
            ControlFlowLabels.ContainsLabelReference(elseBody, joinLabel))
            return false;
        removeJoinLabel = ControlFlowLabels.CountLabelReferences(cfg.Statements, joinLabel) == 1;
        ElseClauseSyntax? elseClause = elseBody.Count == 0 ? null : CreateElseClause(elseBody);
        replacement = CreateIfStatement(condition, thenBody, elseClause);
        contentLength = elseContentEnd - headerIndex;
        return true;
    }

    /// <summary>
    /// Matches lowered empty-else: <c>if not C goto ELSE; then; goto JOIN; [JOIN:/ELSE: in any order]</c>.
    /// Jump tables sort labels by name hash, so co-located ELSE/JOIN often swap on round-trip.
    /// Raises to plain <c>if</c> (no empty else clause). Sibling labels in the same run stay.
    /// </summary>
    private bool TryMatchEmptyElse(
        ControlFlowGraph cfg,
        int headerIndex,
        string elseLabel,
        int elseLabelIndex,
        ExpressionSyntax condition,
        out IfStatementSyntax? replacement,
        out int contentLength,
        out IReadOnlyList<int>? extraRemovals,
        out int joinLabelIndex,
        out bool removeJoinLabel)
    {
        replacement = null;
        contentLength = 0;
        extraRemovals = null;
        joinLabelIndex = -1;
        removeJoinLabel = false;

        // Walk back over co-located labels to the join goto that closes the then arm.
        int cursor = elseLabelIndex;
        while (cursor > headerIndex + 1 && cfg.Statements[cursor - 1] is GotoLabelStatementSyntax)
            cursor--;

        int joinGotoIndex = cursor - 1;
        if (joinGotoIndex <= headerIndex)
            return false;
        if (cfg.Statements[joinGotoIndex] is not GotoStatementSyntax joinGoto ||
            !ControlFlowLabels.TryGetSingleGotoTarget(joinGoto, out string? joinLabel) ||
            joinLabel is null ||
            !ControlFlowLabels.IsNumericJumpLabel(joinLabel))
            return false;
        if (!ControlFlowGraphQueries.TryGetLabelIndex(cfg, joinLabel, out joinLabelIndex))
            return false;

        // Contiguous label run immediately after the join goto must contain BOTH labels.
        int runStart = joinGotoIndex + 1;
        int runEnd = ControlFlowLabelRuns.FindRunEnd(cfg.Statements, runStart);

        if (runEnd - runStart < 2)
            return false;
        if (joinLabelIndex < runStart || joinLabelIndex >= runEnd)
            return false;
        if (elseLabelIndex < runStart || elseLabelIndex >= runEnd)
            return false;

        // No real else body between ELSE and JOIN when JOIN follows ELSE in the stream.
        int elseContentStart = elseLabelIndex + 1;
        int elseContentEnd = ControlFlowGraphQueries.FindContentEndBeforeJoin(
            cfg.Statements, elseContentStart, joinLabelIndex);
        if (joinLabelIndex > elseLabelIndex && elseContentEnd > elseContentStart)
            return false;

        // Nothing after the run before JOIN either (JOIN is inside the run for empty else).
        if (runEnd < cfg.Statements.Count &&
            cfg.Statements[runEnd] is not GotoLabelStatementSyntax &&
            joinLabelIndex >= runEnd)
            return false;

        if (!ControlFlowGraphQueries.TryGetBlockByLabel(cfg, joinLabel, out StatementBlock? joinBlock) ||
            joinBlock is null)
            return false;
        if (!cfg.BlockByStatementIndex.TryGetValue(joinGotoIndex, out StatementBlock? thenTailBlock) ||
            !ControlFlowGraphQueries.HasBranchTo(cfg, thenTailBlock, joinBlock))
            return false;

        var thenBody = ControlFlowGraphQueries.Slice(cfg.Statements, headerIndex + 1, joinGotoIndex);
        if (!ControlFlowGraphQueries.IsStructuredBody(thenBody))
            return false;
        if (ControlFlowLabels.ContainsLabelReference(thenBody, elseLabel) ||
            ControlFlowLabels.ContainsLabelReference(thenBody, joinLabel))
            return false;

        // Replace through the join goto only; remove ELSE/JOIN labels individually so
        // unrelated co-located labels (outer joins) remain.
        contentLength = joinGotoIndex + 1 - headerIndex;
        extraRemovals = ControlFlowLabelRuns.IndicesOfLabels(
            cfg.Statements,
            runStart,
            runEnd,
            name => name == elseLabel || name == joinLabel);
        removeJoinLabel = false;
        replacement = CreateIfStatement(condition, thenBody, elseClause: null);
        return true;
    }

    private bool TryMatchIfThen(
        ControlFlowGraph cfg,
        int headerIndex,
        out IfStatementSyntax? replacement,
        out int contentLength,
        out int endLabelIndex,
        out bool removeEndLabel)
    {
        replacement = null;
        contentLength = 0;
        endLabelIndex = -1;
        removeEndLabel = false;
        if (!cfg.BlockByStatementIndex.TryGetValue(headerIndex, out StatementBlock? headerBlock))
            return false;
        if (!ControlFlowGraphQueries.TryGetBranchSkip(
                cfg.Statements[headerIndex],
                syntaxFactory,
                out ExpressionSyntax? condition,
                out string? endLabel) ||
            condition is null ||
            endLabel is null)
            return false;
        if (!ControlFlowLabels.IsNumericJumpLabel(endLabel))
            return false;
        if (!ControlFlowGraphQueries.TryGetLabelIndex(cfg, endLabel, out endLabelIndex) ||
            endLabelIndex <= headerIndex)
            return false;
        if (!ControlFlowGraphQueries.TryGetBlockByLabel(cfg, endLabel, out StatementBlock? endBlock) ||
            endBlock is null)
            return false;
        if (!ControlFlowGraphQueries.HasBranchTo(cfg, headerBlock, endBlock) ||
            !ControlFlowGraphQueries.HasFallThrough(cfg, headerBlock))
            return false;
        if (ControlFlowLabels.CountLabelReferences(cfg.Statements, endLabel) != 1)
            return false;
        // Same coalesced-join case as if/else: an outer join label may sit between the then
        // body and this end label (e.g. "@014@": before "@023@":).
        int thenContentEnd = ControlFlowGraphQueries.FindContentEndBeforeJoin(
            cfg.Statements, headerIndex + 1, endLabelIndex);
        var thenBody = ControlFlowGraphQueries.Slice(cfg.Statements, headerIndex + 1, thenContentEnd);
        if (!ControlFlowGraphQueries.IsStructuredBody(thenBody))
            return false;
        if (ControlFlowLabels.ContainsLabelReference(thenBody, endLabel))
            return false;
        removeEndLabel = true;
        replacement = CreateIfStatement(condition, thenBody, elseClause: null);
        contentLength = thenContentEnd - headerIndex;
        return true;
    }
    private ElseClauseSyntax CreateElseClause(IReadOnlyList<StatementSyntax> elseBody)
    {
        // Collapse a single nested if into `else if`.
        if (elseBody.Count == 1 && elseBody[0] is IfStatementSyntax nestedIf)
            return new ElseClauseSyntax(syntaxFactory.Token(SyntaxTokenKind.ElseKeyword), nestedIf);
        return new ElseClauseSyntax(
            syntaxFactory.Token(SyntaxTokenKind.ElseKeyword),
            StructuredSyntaxRecursor.CreateBlock(elseBody, syntaxFactory));
    }
    private IfStatementSyntax CreateIfStatement(
        ExpressionSyntax condition,
        IReadOnlyList<StatementSyntax> thenBody,
        ElseClauseSyntax? elseClause)
    {
        return new IfStatementSyntax(
            syntaxFactory.Token(SyntaxTokenKind.IfKeyword),
            condition,
            StructuredSyntaxRecursor.CreateBlock(thenBody, syntaxFactory),
            elseClause);
    }
}
