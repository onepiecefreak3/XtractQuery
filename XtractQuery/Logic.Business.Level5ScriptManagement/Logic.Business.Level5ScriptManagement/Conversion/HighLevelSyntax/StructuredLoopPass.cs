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

                if (TryMatchSpinLoop(cfg, headIndex, headLabel, out LoopRaise? spin) && spin is not null)
                {
                    ApplyLoopRaise(result, spin);
                    changed = true;
                    break;
                }

                if (TryMatchTopTestedWhile(cfg, headIndex, headLabel, out LoopRaise? topWhile) && topWhile is not null)
                {
                    ApplyLoopRaise(result, topWhile);
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

    private static void ApplyLoopRaise(List<StatementSyntax> result, LoopRaise raise)
    {
        if (raise.ExitLabelIndex >= 0)
            result.RemoveAt(raise.ExitLabelIndex);

        result.RemoveRange(raise.ReplaceStart, raise.ReplaceLength);

        var insert = new List<StatementSyntax>(raise.KeptPrefix.Count + 1);
        insert.AddRange(raise.KeptPrefix);
        insert.Add(raise.Replacement);
        result.InsertRange(raise.ReplaceStart, insert);
    }

    private sealed class LoopRaise
    {
        public required int ReplaceStart { get; init; }
        public required int ReplaceLength { get; init; }
        public required IReadOnlyList<StatementSyntax> KeptPrefix { get; init; }
        public required StatementSyntax Replacement { get; init; }
        public int ExitLabelIndex { get; init; } = -1;
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
        out LoopRaise? raise)
    {
        raise = null;

        // Co-located labels share one instruction after jump-table hash sort.
        int runStart = ControlFlowLabelRuns.FindRunStart(cfg.Statements, headIndex);
        if (runStart != headIndex)
            return false;

        int runEnd = ControlFlowLabelRuns.FindRunEnd(cfg.Statements, headIndex);
        if (runEnd >= cfg.Statements.Count)
            return false;

        HashSet<string> headerLabels = ControlFlowLabelRuns.CollectLabels(cfg.Statements, runStart, runEnd);
        if (!headerLabels.Contains(headLabel))
            return false;

        StatementSyntax candidate = cfg.Statements[runEnd];
        ExpressionSyntax? condition = null;
        string? backTarget = null;
        if (candidate is IfGotoStatementSyntax ifGoto &&
            ControlFlowLabels.TryGetLabelName(ifGoto.Goto.Target, out backTarget) &&
            backTarget is not null &&
            headerLabels.Contains(backTarget))
        {
            condition = ControlFlowLabels.UnwrapCondition(ifGoto.Value);
        }
        else if (candidate is IfNotGotoStatementSyntax ifNotGoto &&
                 ControlFlowLabels.TryGetLabelName(ifNotGoto.Goto.Target, out backTarget) &&
                 backTarget is not null &&
                 headerLabels.Contains(backTarget))
        {
            condition = ifNotGoto.Comparison;
        }
        else
            return false;

        if (!cfg.BlockByStatementIndex.TryGetValue(runEnd, out StatementBlock? branchBlock))
            return false;
        if (!ControlFlowGraphQueries.TryGetBlockByLabel(cfg, backTarget, out StatementBlock? targetBlock) ||
            targetBlock is null)
            return false;
        // Spin branches back into the co-located header region (same instruction cluster).
        if (!ControlFlowGraphQueries.HasBranchTo(cfg, branchBlock, targetBlock) &&
            !ControlFlowGraphQueries.HasBranchTo(cfg, branchBlock, branchBlock))
            return false;

        // Back-edge target may only be referenced by this spin branch.
        if (ControlFlowLabels.CountLabelReferences(cfg.Statements, backTarget) != 1)
            return false;

        int termIndex = runEnd;
        IReadOnlyList<StatementSyntax> keptPrefix = CollectKeptHeaderLabels(
            cfg.Statements, runStart, runEnd, backTarget, loopStart: runStart, loopEnd: termIndex);

        raise = new LoopRaise
        {
            ReplaceStart = runStart,
            ReplaceLength = termIndex - runStart + 1,
            KeptPrefix = keptPrefix,
            Replacement = CreateWhileOneLiner(condition)
        };
        return true;
    }

    private bool TryMatchTopTestedWhile(
        ControlFlowGraph cfg,
        int headIndex,
        string headLabel,
        out LoopRaise? raise)
    {
        raise = null;

        int runStart = ControlFlowLabelRuns.FindRunStart(cfg.Statements, headIndex);
        if (runStart != headIndex)
            return false;

        int runEnd = ControlFlowLabelRuns.FindRunEnd(cfg.Statements, headIndex);
        if (runEnd >= cfg.Statements.Count)
            return false;

        HashSet<string> headerLabels = ControlFlowLabelRuns.CollectLabels(cfg.Statements, runStart, runEnd);

        StatementSyntax terminator = cfg.Statements[runEnd];
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
        if (!ControlFlowGraphQueries.TryGetLabelIndex(cfg, exitLabel, out int exitLabelIndex) ||
            exitLabelIndex <= runEnd)
            return false;
        if (!ControlFlowGraphQueries.TryGetBlockByLabel(cfg, exitLabel, out StatementBlock? exitBlock) ||
            exitBlock is null)
            return false;
        if (!cfg.BlockByStatementIndex.TryGetValue(runEnd, out StatementBlock? branchBlock))
            return false;
        if (!ControlFlowGraphQueries.HasBranchTo(cfg, branchBlock, exitBlock) ||
            !ControlFlowGraphQueries.HasFallThrough(cfg, branchBlock))
            return false;
        if (ControlFlowLabels.CountLabelReferences(cfg.Statements, exitLabel) < 1)
            return false;

        int bodyStart = runEnd + 1;
        int bodyEnd = ControlFlowGraphQueries.FindContentEndBeforeJoin(
            cfg.Statements, bodyStart, exitLabelIndex);
        var rawBody = ControlFlowGraphQueries.Slice(cfg.Statements, bodyStart, bodyEnd);
        if (rawBody.Count == 0)
            return false;
        if (rawBody[^1] is not GotoStatementSyntax backEdge ||
            !ControlFlowLabels.TryGetSingleGotoTarget(backEdge, out string? backTarget) ||
            backTarget is null ||
            !headerLabels.Contains(backTarget))
            return false;

        // Canonical head is whatever the back-edge targets (may differ from headIndex label).
        string canonicalHead = backTarget;

        int backEdgeIndex = bodyEnd - 1;
        if (!cfg.BlockByStatementIndex.TryGetValue(backEdgeIndex, out StatementBlock? backBlock) ||
            !ControlFlowGraphQueries.TryGetBlockByLabel(cfg, canonicalHead, out StatementBlock? headTargetBlock) ||
            headTargetBlock is null ||
            !ControlFlowGraphQueries.HasBranchTo(cfg, backBlock, headTargetBlock))
            return false;

        var bodyWithoutBackEdge = rawBody.Take(rawBody.Count - 1).ToList();
        if (!IsValidLoopBody(bodyWithoutBackEdge, canonicalHead, exitLabel, cfg.Statements, runStart, bodyEnd - 1))
            return false;

        int headRefsOutsideBody = ControlFlowLabels.CountLabelReferencesOutsideRange(
            cfg.Statements, canonicalHead, bodyStart, bodyEnd);
        if (headRefsOutsideBody != 0)
            return false;

        IReadOnlyList<StatementSyntax> rewrittenBody = RewriteLoopBody(bodyWithoutBackEdge, canonicalHead, exitLabel);
        ExpressionSyntax whileCondition = ControlFlowGraphQueries.IsLiteralOne(condition)
            ? CreateTrueLiteral()
            : condition;
        int exitRefsInBody = ControlFlowLabels.CountLabelReferences(bodyWithoutBackEdge, exitLabel);
        int exitRefsTotal = ControlFlowLabels.CountLabelReferences(cfg.Statements, exitLabel);
        bool removeExitLabel = exitRefsTotal == exitRefsInBody + 1;

        IReadOnlyList<StatementSyntax> keptPrefix = CollectKeptHeaderLabels(
            cfg.Statements, runStart, runEnd, canonicalHead, loopStart: runStart, loopEnd: bodyEnd - 1);

        raise = new LoopRaise
        {
            ReplaceStart = runStart,
            ReplaceLength = bodyEnd - runStart,
            KeptPrefix = keptPrefix,
            Replacement = CreateWhile(whileCondition, rewrittenBody),
            ExitLabelIndex = removeExitLabel ? exitLabelIndex : -1
        };
        return true;
    }

    /// <summary>
    /// Labels co-located with a loop head that are still referenced from outside the loop
    /// (e.g. an if-else target sharing the spin instruction) must stay in the stream.
    /// </summary>
    private static IReadOnlyList<StatementSyntax> CollectKeptHeaderLabels(
        IReadOnlyList<StatementSyntax> statements,
        int runStart,
        int runEnd,
        string loopHeadLabel,
        int loopStart,
        int loopEnd)
    {
        var kept = new List<StatementSyntax>();
        for (int i = runStart; i < runEnd; i++)
        {
            if (!ControlFlowLabels.TryGetLabelDefinition(statements[i], out string? name) || name is null)
                continue;
            if (name == loopHeadLabel)
                continue;
            if (ControlFlowLabels.CountLabelReferencesOutsideRange(statements, name, loopStart, loopEnd + 1) > 0)
                kept.Add(statements[i]);
        }

        return kept;
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
