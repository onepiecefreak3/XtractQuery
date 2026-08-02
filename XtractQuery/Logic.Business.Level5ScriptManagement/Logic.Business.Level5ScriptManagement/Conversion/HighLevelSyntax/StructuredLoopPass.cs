using Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;
using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Raises top-tested / spin / do-while loops. Natural-loop regions (innermost first)
/// drive candidate ordering; matching still uses CFG shape rules so break-only blocks
/// omitted from classic latch-reachability bodies do not block raising.
/// </summary>
internal class StructuredLoopPass(
    IControlFlowGraphBuilder cfgBuilder,
    IControlFlowRegionAnalyzer regionAnalyzer,
    ILevel5SyntaxFactory syntaxFactory) : IStructuredLoopPass
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

            foreach (int headIndex in CollectLoopHeadCandidates(cfg, regions))
            {
                if (!ControlFlowLabels.TryGetLabelDefinition(cfg.Statements[headIndex], out string? headLabel) ||
                    headLabel is null ||
                    !ControlFlowLabels.IsNumericJumpLabel(headLabel))
                    continue;

                if (TryMatchSpinLoop(cfg, headIndex, headLabel, out WhileStatementSyntax? spin, out int spinLength) &&
                    spin is not null)
                {
                    result.RemoveRange(headIndex, spinLength);
                    result.Insert(headIndex, spin);
                    changed = true;
                    break;
                }

                if (TryMatchTopTestedWhile(
                        cfg,
                        headIndex,
                        headLabel,
                        out WhileStatementSyntax? topWhile,
                        out int topLength,
                        out int exitLabelIndex,
                        out bool removeExitLabel) &&
                    topWhile is not null)
                {
                    // Exit and any co-located fallthrough labels sit after the loop content.
                    // Remove the exit first (higher index), then replace the content span so
                    // intervening labels such as an outer if-join stay in the stream.
                    if (removeExitLabel)
                        result.RemoveAt(exitLabelIndex);

                    result.RemoveRange(headIndex, topLength);
                    result.Insert(headIndex, topWhile);
                    changed = true;
                    break;
                }

                if (TryMatchDoWhile(cfg, headIndex, headLabel, out DoWhileStatementSyntax? doWhile, out int doLength) &&
                    doWhile is not null)
                {
                    result.RemoveRange(headIndex, doLength);
                    result.Insert(headIndex, doWhile);
                    changed = true;
                    break;
                }
            }
        }

        return result;
    }

    private static IEnumerable<int> CollectLoopHeadCandidates(ControlFlowGraph cfg, ControlFlowRegions regions)
    {
        var seen = new HashSet<int>();

        // Innermost natural loops first (analyzer orders by ascending body size).
        foreach (NaturalLoop loop in regions.Loops)
        {
            int index = loop.Header.InstructionIndex;
            if (seen.Add(index))
                yield return index;
        }

        foreach (StatementBlock block in cfg.Blocks)
        {
            if (block.IsExit || block.StatementCount <= 0)
                continue;

            if (!seen.Add(block.InstructionIndex))
                continue;

            yield return block.InstructionIndex;
        }
    }

    private bool TryMatchSpinLoop(
        ControlFlowGraph cfg,
        int headIndex,
        string headLabel,
        out WhileStatementSyntax? replacement,
        out int length)
    {
        replacement = null;
        length = 0;
        if (headIndex + 1 >= cfg.Statements.Count)
            return false;
        if (!cfg.BlockByStatementIndex.TryGetValue(headIndex, out StatementBlock? headBlock) ||
            !ControlFlowGraphQueries.TryGetBlockByLabel(cfg, headLabel, out StatementBlock? labelBlock) ||
            labelBlock is null ||
            !ReferenceEquals(headBlock, labelBlock))
            return false;

        // Classic spin shape: one basic block containing only the label and the back-edge branch.
        if (headBlock.StatementCount != 2)
            return false;

        ExpressionSyntax? condition = null;
        StatementSyntax candidate = cfg.Statements[headIndex + 1];
        if (candidate is IfGotoStatementSyntax ifGoto &&
            ControlFlowLabels.TryGetLabelName(ifGoto.Goto.Target, out string? target) &&
            target == headLabel)
        {
            condition = ControlFlowLabels.UnwrapCondition(ifGoto.Value);
        }
        else if (candidate is IfNotGotoStatementSyntax ifNotGoto &&
                 ControlFlowLabels.TryGetLabelName(ifNotGoto.Goto.Target, out string? notTarget) &&
                 notTarget == headLabel)
        {
            condition = ifNotGoto.Comparison;
        }
        else
            return false;
        // Spin header block must branch/jump back to itself; no fallthrough body.
        if (!ControlFlowGraphQueries.HasBranchTo(cfg, headBlock, headBlock))
            return false;
        if (ControlFlowLabels.CountLabelReferences(cfg.Statements, headLabel) != 1)
            return false;
        replacement = CreateWhileOneLiner(condition);
        length = 2;
        return true;
    }
    private bool TryMatchTopTestedWhile(
        ControlFlowGraph cfg,
        int headIndex,
        string headLabel,
        out WhileStatementSyntax? replacement,
        out int length,
        out int exitLabelIndex,
        out bool removeExitLabel)
    {
        replacement = null;
        length = 0;
        exitLabelIndex = -1;
        removeExitLabel = false;
        if (!cfg.BlockByStatementIndex.TryGetValue(headIndex, out StatementBlock? headBlock))
            return false;

        // Classic while header: one block with exactly the label and the exit branch.
        if (headBlock.StatementCount != 2 || headIndex + 1 >= cfg.Statements.Count)
            return false;

        StatementSyntax terminator = cfg.Statements[headIndex + 1];
        ExpressionSyntax? condition;
        string? exitLabel;
        if (TryGetIfNotGoto(terminator, out ExpressionSyntax? positiveCondition, out exitLabel) &&
            positiveCondition is not null && exitLabel is not null)
        {
            condition = positiveCondition;
        }
        else if (terminator is IfGotoStatementSyntax ifGoto &&
                 ControlFlowLabels.TryGetLabelName(ifGoto.Goto.Target, out exitLabel) &&
                 exitLabel is not null)
        {
            // L: if cond goto EXIT; body; goto L; EXIT:  ==  while (not cond) { body }
            condition = new UnaryExpressionSyntax(
                syntaxFactory.Token(SyntaxTokenKind.NotKeyword),
                ControlFlowGraphQueries.RequireValueExpression(
                    ControlFlowLabels.UnwrapCondition(ifGoto.Value)));
        }
        else
            return false;
        if (!ControlFlowLabels.IsNumericJumpLabel(exitLabel))
            return false;
        if (!ControlFlowGraphQueries.TryGetLabelIndex(cfg, exitLabel, out exitLabelIndex) ||
            exitLabelIndex <= headIndex)
            return false;
        if (!ControlFlowGraphQueries.TryGetBlockByLabel(cfg, exitLabel, out StatementBlock? exitBlock) ||
            exitBlock is null)
            return false;
        if (!ControlFlowGraphQueries.HasBranchTo(cfg, headBlock, exitBlock) ||
            !ControlFlowGraphQueries.HasFallThrough(cfg, headBlock))
            return false;
        if (ControlFlowLabels.CountLabelReferences(cfg.Statements, exitLabel) < 1)
            return false;
        // Nested while often shares a merge instruction with an outer if-join, so other
        // fallthrough labels may sit between the back-edge and this exit (e.g. "@003@":
        // before "@005@":). Those labels are not part of the loop body and must stay put.
        int bodyStart = headBlock.EndStatementIndex;
        int bodyEnd = ControlFlowGraphQueries.FindContentEndBeforeJoin(
            cfg.Statements, bodyStart, exitLabelIndex);
        var rawBody = ControlFlowGraphQueries.Slice(cfg.Statements, bodyStart, bodyEnd);
        if (rawBody.Count == 0)
            return false;
        if (rawBody[^1] is not GotoStatementSyntax backEdge ||
            !ControlFlowLabels.TryGetSingleGotoTarget(backEdge, out string? backTarget) ||
            backTarget != headLabel)
            return false;
        int backEdgeIndex = bodyEnd - 1;
        if (!cfg.BlockByStatementIndex.TryGetValue(backEdgeIndex, out StatementBlock? backBlock) ||
            !ControlFlowGraphQueries.HasBranchTo(cfg, backBlock, headBlock))
            return false;
        var bodyWithoutBackEdge = rawBody.Take(rawBody.Count - 1).ToList();
        if (!IsValidLoopBody(bodyWithoutBackEdge, headLabel, exitLabel, cfg.Statements, headIndex, bodyEnd - 1))
            return false;
        // Head may only be targeted by the trailing back-edge and continues inside the body.
        int headRefsOutsideBody = ControlFlowLabels.CountLabelReferencesOutsideRange(
            cfg.Statements, headLabel, bodyStart, bodyEnd);
        if (headRefsOutsideBody != 0)
            return false;
        IReadOnlyList<StatementSyntax> rewrittenBody = RewriteLoopBody(bodyWithoutBackEdge, headLabel, exitLabel);
        ExpressionSyntax whileCondition = ControlFlowGraphQueries.IsLiteralOne(condition)
            ? CreateTrueLiteral()
            : condition;
        int exitRefsInBody = ControlFlowLabels.CountLabelReferences(bodyWithoutBackEdge, exitLabel);
        int exitRefsTotal = ControlFlowLabels.CountLabelReferences(cfg.Statements, exitLabel);
        // Header if-not contributes 1; body contributes breaks. After rewrite those become break.
        removeExitLabel = exitRefsTotal == exitRefsInBody + 1;
        replacement = CreateWhile(whileCondition, rewrittenBody);
        // Replace head..back-edge only; coalesced join labels between bodyEnd and exit stay.
        length = bodyEnd - headIndex;
        return true;
    }
    private bool TryMatchDoWhile(
        ControlFlowGraph cfg,
        int headIndex,
        string headLabel,
        out DoWhileStatementSyntax? replacement,
        out int length)
    {
        replacement = null;
        length = 0;
        if (!cfg.BlockByStatementIndex.TryGetValue(headIndex, out StatementBlock? headBlock) ||
            !ControlFlowGraphQueries.TryGetBlockByLabel(cfg, headLabel, out StatementBlock? labelBlock) ||
            labelBlock is null ||
            !ReferenceEquals(headBlock, labelBlock))
            return false;
        // Find a trailing if-goto back to head with only structured body between.
        // Prefer CFG predecessors of the head that end in IfGoto.
        foreach (ControlFlowEdge incoming in ControlFlowGraphQueries.GetIncoming(cfg, headBlock))
        {
            if (incoming.Kind is not (ControlFlowEdgeKind.Branch or ControlFlowEdgeKind.Jump))
                continue;
            if (ReferenceEquals(incoming.Source, headBlock))
                continue;
            if (!ControlFlowGraphQueries.TryGetTerminator(cfg, incoming.Source, out StatementSyntax? terminator) ||
                terminator is not IfGotoStatementSyntax ifGoto ||
                !ControlFlowLabels.TryGetLabelName(ifGoto.Goto.Target, out string? target) ||
                target != headLabel)
                continue;
            int end = incoming.Source.EndStatementIndex - 1;
            if (end <= headIndex)
                continue;
            var body = ControlFlowGraphQueries.Slice(cfg.Statements, headIndex + 1, end);
            if (body.Count == 0)
                continue;
            if (!IsValidLoopBody(body, headLabel, exitLabel: null, cfg.Statements, headIndex, end))
                continue;
            if (ControlFlowLabels.CountLabelReferencesOutsideRange(
                    cfg.Statements, headLabel, headIndex + 1, end + 1) != 0)
                continue;
            // Exactly one reference: this if-goto (continues inside body would be more).
            int headRefs = ControlFlowLabels.CountLabelReferences(cfg.Statements, headLabel);
            int bodyContinues = ControlFlowLabels.CountLabelReferences(body, headLabel);
            if (headRefs != bodyContinues + 1)
                continue;
            IReadOnlyList<StatementSyntax> rewritten = RewriteLoopBody(body, headLabel, exitLabel: null);
            replacement = CreateDoWhile(ControlFlowLabels.UnwrapCondition(ifGoto.Value), rewritten);
            length = end - headIndex + 1;
            return true;
        }
        return false;
    }
    private static bool IsValidLoopBody(
        IReadOnlyList<StatementSyntax> body,
        string headLabel,
        string? exitLabel,
        IReadOnlyList<StatementSyntax> allStatements,
        int loopStart,
        int loopEnd)
    {
        HashSet<string> internalLabels = ControlFlowGraphQueries.CollectDefinedLabels(body);
        foreach (StatementSyntax statement in body)
        {
            if (!AreJumpTargetsAllowed(statement, headLabel, exitLabel, internalLabels))
                return false;
        }
        // Compiler-generated labels must not be entered from outside the loop.
        // Explicit developer labels are kept as-is and may still be targeted externally.
        foreach (string label in internalLabels)
        {
            if (!ControlFlowLabels.IsNumericJumpLabel(label))
                continue;
            if (ControlFlowLabels.CountLabelReferencesOutsideRange(
                    allStatements, label, loopStart, loopEnd + 1) != 0)
                return false;
        }
        return true;
    }
    private static bool AreJumpTargetsAllowed(
        StatementSyntax statement,
        string headLabel,
        string? exitLabel,
        IReadOnlySet<string> internalLabels)
    {
        switch (statement)
        {
            case GotoStatementSyntax gotoStatement:
                return gotoStatement.Targets.Elements.All(t =>
                    IsAllowedTarget(t, headLabel, exitLabel, internalLabels));
            case IfGotoStatementSyntax ifGoto:
                return IsAllowedTarget(ifGoto.Goto.Target, headLabel, exitLabel, internalLabels);
            case IfNotGotoStatementSyntax ifNotGoto:
                return IsAllowedTarget(ifNotGoto.Goto.Target, headLabel, exitLabel, internalLabels);
            case GotoLabelStatementSyntax:
            case AssignmentStatementSyntax:
            case MethodInvocationStatementSyntax:
            case YieldStatementSyntax:
            case ReturnStatementSyntax:
            case ExitStatementSyntax:
            case PostfixUnaryStatementSyntax:
            case BreakStatementSyntax:
            case ContinueStatementSyntax:
            case WhileStatementSyntax:
            case DoWhileStatementSyntax:
            case IfStatementSyntax:
                return true;
            default:
                return false;
        }
    }
    private static bool IsAllowedTarget(
        ValueExpressionSyntax target,
        string headLabel,
        string? exitLabel,
        IReadOnlySet<string> internalLabels)
    {
        if (!ControlFlowLabels.TryGetLabelName(target, out string? name) || name is null)
            return false;
        if (name == headLabel)
            return true;
        if (exitLabel is not null && name == exitLabel)
            return true;
        if (internalLabels.Contains(name))
            return true;
        // Explicit developer labels (including targets outside the loop) stay as gotos.
        return ControlFlowLabels.IsDeveloperLabel(name);
    }
    private IReadOnlyList<StatementSyntax> RewriteLoopBody(
        IReadOnlyList<StatementSyntax> body,
        string headLabel,
        string? exitLabel)
    {
        var result = new List<StatementSyntax>();
        foreach (StatementSyntax statement in body)
            result.Add(RewriteLoopStatement(statement, headLabel, exitLabel));
        return result;
    }
    private StatementSyntax RewriteLoopStatement(StatementSyntax statement, string headLabel, string? exitLabel)
    {
        switch (statement)
        {
            case GotoStatementSyntax gotoStatement when ControlFlowLabels.TryGetSingleGotoTarget(gotoStatement, out string? target):
                if (exitLabel is not null && target == exitLabel)
                    return CreateBreak();
                if (target == headLabel)
                    return CreateContinue();
                return statement;
            default:
                return statement;
        }
    }
    private WhileStatementSyntax CreateWhile(ExpressionSyntax condition, IReadOnlyList<StatementSyntax> body)
    {
        if (body.Count == 0)
            return CreateWhileOneLiner(condition);
        return new WhileStatementSyntax(
            syntaxFactory.Token(SyntaxTokenKind.WhileKeyword),
            syntaxFactory.Token(SyntaxTokenKind.ParenOpen),
            condition,
            syntaxFactory.Token(SyntaxTokenKind.ParenClose),
            StructuredSyntaxRecursor.CreateBlock(body, syntaxFactory),
            null);
    }
    private WhileStatementSyntax CreateWhileOneLiner(ExpressionSyntax condition)
    {
        return new WhileStatementSyntax(
            syntaxFactory.Token(SyntaxTokenKind.WhileKeyword),
            syntaxFactory.Token(SyntaxTokenKind.ParenOpen),
            condition,
            syntaxFactory.Token(SyntaxTokenKind.ParenClose),
            null,
            syntaxFactory.Token(SyntaxTokenKind.Semicolon));
    }
    private DoWhileStatementSyntax CreateDoWhile(ExpressionSyntax condition, IReadOnlyList<StatementSyntax> body)
    {
        return new DoWhileStatementSyntax(
            syntaxFactory.Token(SyntaxTokenKind.DoKeyword),
            StructuredSyntaxRecursor.CreateBlock(body, syntaxFactory),
            syntaxFactory.Token(SyntaxTokenKind.WhileKeyword),
            syntaxFactory.Token(SyntaxTokenKind.ParenOpen),
            condition,
            syntaxFactory.Token(SyntaxTokenKind.ParenClose),
            syntaxFactory.Token(SyntaxTokenKind.Semicolon));
    }
    private BreakStatementSyntax CreateBreak()
    {
        return new BreakStatementSyntax(
            syntaxFactory.Token(SyntaxTokenKind.BreakKeyword),
            syntaxFactory.Token(SyntaxTokenKind.Semicolon));
    }
    private ContinueStatementSyntax CreateContinue()
    {
        return new ContinueStatementSyntax(
            syntaxFactory.Token(SyntaxTokenKind.ContinueKeyword),
            syntaxFactory.Token(SyntaxTokenKind.Semicolon));
    }
    private ExpressionSyntax CreateTrueLiteral()
    {
        return new LiteralExpressionSyntax(syntaxFactory.Token(SyntaxTokenKind.TrueKeyword));
    }
    private static bool TryGetIfNotGoto(
        StatementSyntax statement,
        out ExpressionSyntax? positiveCondition,
        out string? targetLabel)
    {
        positiveCondition = null;
        targetLabel = null;
        if (statement is not IfNotGotoStatementSyntax ifNotGoto)
            return false;
        if (!ControlFlowLabels.TryGetLabelName(ifNotGoto.Goto.Target, out targetLabel) || targetLabel is null)
            return false;
        positiveCondition = ControlFlowLabels.UnwrapCondition(ifNotGoto.Comparison.Value);
        return true;
    }
}
