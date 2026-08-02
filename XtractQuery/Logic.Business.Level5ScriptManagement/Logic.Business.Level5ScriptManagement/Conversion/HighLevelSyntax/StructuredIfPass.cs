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
                        out int ifElseJoinIndex,
                        out bool removeIfElseJoin) &&
                    ifElse is not null)
                {
                    // Join and any co-located fallthrough labels sit after the if/else content.
                    // Remove the join first (higher index), then replace the content span so
                    // intervening labels such as an outer join stay in the stream.
                    if (removeIfElseJoin)
                        result.RemoveAt(ifElseJoinIndex);

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
        out int joinLabelIndex,
        out bool removeJoinLabel)
    {
        replacement = null;
        contentLength = 0;
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
        replacement = CreateIfStatement(condition, thenBody, CreateElseClause(elseBody));
        contentLength = elseContentEnd - headerIndex;
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
