using Logic.Business.Level5ScriptManagement.DataClasses.Conversion;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;

/// <summary>
/// Query helpers over a shared <see cref="ControlFlowGraph"/> used by all raising passes.
/// </summary>
internal static class ControlFlowGraphQueries
{
    public static bool TryGetTerminator(ControlFlowGraph cfg, StatementBlock block, out StatementSyntax? terminator)
    {
        terminator = null;
        if (block.IsExit || block.StatementCount <= 0)
            return false;

        int lastIndex = block.EndStatementIndex - 1;
        if (lastIndex < 0 || lastIndex >= cfg.Statements.Count)
            return false;

        terminator = cfg.Statements[lastIndex];
        return true;
    }

    public static bool TryGetOutgoing(
        ControlFlowGraph cfg,
        StatementBlock block,
        ControlFlowEdgeKind kind,
        out ControlFlowEdge? edge)
    {
        edge = cfg.OutgoingEdges[block].FirstOrDefault(e => e.Kind == kind);
        return edge is not null;
    }

    public static IReadOnlyList<ControlFlowEdge> GetOutgoing(ControlFlowGraph cfg, StatementBlock block)
    {
        return cfg.OutgoingEdges[block];
    }

    public static IReadOnlyList<ControlFlowEdge> GetIncoming(ControlFlowGraph cfg, StatementBlock block)
    {
        return cfg.IncomingEdges[block];
    }

    /// <summary>
    /// Conditional skip used by structured if/while headers:
    /// <c>if not X goto L</c> ≡ then-condition X;
    /// <c>if X goto L</c> ≡ then-condition not X.
    /// </summary>
    public static bool TryGetBranchSkip(
        StatementSyntax statement,
        ILevel5SyntaxFactory syntaxFactory,
        out ExpressionSyntax? thenCondition,
        out string? targetLabel)
    {
        thenCondition = null;
        targetLabel = null;

        if (statement is IfNotGotoStatementSyntax ifNotGoto)
        {
            if (!ControlFlowLabels.TryGetLabelName(ifNotGoto.Goto.Target, out targetLabel) || targetLabel is null)
                return false;

            thenCondition = ControlFlowLabels.UnwrapCondition(ifNotGoto.Comparison.Value);
            return true;
        }

        if (statement is IfGotoStatementSyntax ifGoto)
        {
            if (!ControlFlowLabels.TryGetLabelName(ifGoto.Goto.Target, out targetLabel) || targetLabel is null)
                return false;

            thenCondition = CreateNotCondition(ControlFlowLabels.UnwrapCondition(ifGoto.Value), syntaxFactory);
            return true;
        }

        return false;
    }

    public static ExpressionSyntax CreateNotCondition(ExpressionSyntax expression, ILevel5SyntaxFactory syntaxFactory)
    {
        ExpressionSyntax operand = ExpressionParenthesizer.MaybeParenthesize(
            expression,
            ExpressionPrecedence.Unary,
            isRightOperand: true,
            syntaxFactory);

        if (operand is ParenthesizedExpressionSyntax)
            return new UnaryExpressionSyntax(syntaxFactory.Token(SyntaxTokenKind.NotKeyword), operand);

        ValueExpressionSyntax value = operand is ValueExpressionSyntax valueExpression
            ? valueExpression
            : new ValueExpressionSyntax(operand);

        return new UnaryExpressionSyntax(syntaxFactory.Token(SyntaxTokenKind.NotKeyword), value);
    }

    public static bool TryGetLabelIndex(ControlFlowGraph cfg, string labelName, out int index)
    {
        return cfg.LabelStatementIndex.TryGetValue(labelName, out index);
    }

    public static bool TryGetBlockByLabel(ControlFlowGraph cfg, string labelName, out StatementBlock? block)
    {
        return cfg.BlockByLabel.TryGetValue(labelName, out block);
    }

    /// <summary>
    /// Exclusive end of body statements, skipping trailing labels that share a join/exit
    /// instruction (compiler <c>@NNN@</c> joins and co-located developer labels).
    /// </summary>
    public static int FindContentEndBeforeJoin(
        IReadOnlyList<StatementSyntax> statements,
        int contentStart,
        int joinLabelIndex)
    {
        int end = joinLabelIndex;
        while (end > contentStart && statements[end - 1] is GotoLabelStatementSyntax)
            end--;

        return end;
    }

    public static List<StatementSyntax> Slice(
        IReadOnlyList<StatementSyntax> statements,
        int startInclusive,
        int endExclusive)
    {
        if (endExclusive <= startInclusive)
            return [];

        return statements.Skip(startInclusive).Take(endExclusive - startInclusive).ToList();
    }

    public static bool AreInSameBlock(ControlFlowGraph cfg, int firstIndex, int secondIndex)
    {
        if (!cfg.BlockByStatementIndex.TryGetValue(firstIndex, out StatementBlock? firstBlock) ||
            !cfg.BlockByStatementIndex.TryGetValue(secondIndex, out StatementBlock? secondBlock))
            return false;

        return ReferenceEquals(firstBlock, secondBlock);
    }

    public static bool AreConsecutiveInSameBlock(ControlFlowGraph cfg, int firstIndex, int secondIndex)
    {
        return secondIndex == firstIndex + 1 && AreInSameBlock(cfg, firstIndex, secondIndex);
    }

    public static int CountJumpEdgesToLabel(ControlFlowGraph cfg, string labelName)
    {
        if (!cfg.BlockByLabel.TryGetValue(labelName, out StatementBlock? target))
            return 0;

        return cfg.IncomingEdges[target]
            .Count(e => e.Kind is ControlFlowEdgeKind.Branch or ControlFlowEdgeKind.Jump);
    }

    public static bool HasFallThrough(ControlFlowGraph cfg, StatementBlock block)
    {
        return TryGetOutgoing(cfg, block, ControlFlowEdgeKind.FallThrough, out _);
    }

    public static bool HasBranchTo(ControlFlowGraph cfg, StatementBlock source, StatementBlock target)
    {
        return cfg.OutgoingEdges[source].Any(e =>
            e.Kind is ControlFlowEdgeKind.Branch or ControlFlowEdgeKind.Jump &&
            ReferenceEquals(e.Target, target));
    }

    public static HashSet<string> CollectDefinedLabels(IReadOnlyList<StatementSyntax> statements)
    {
        var labels = new HashSet<string>(StringComparer.Ordinal);
        foreach (StatementSyntax statement in statements)
        {
            if (ControlFlowLabels.TryGetLabelDefinition(statement, out string? name) && name is not null)
                labels.Add(name);
        }

        return labels;
    }

    public static bool IsStructuredBody(IReadOnlyList<StatementSyntax> statements)
    {
        return statements.All(IsStructuredBodyStatement);
    }

    public static bool IsStructuredBodyStatement(StatementSyntax statement)
    {
        switch (statement)
        {
            case AssignmentStatementSyntax:
            case MethodInvocationStatementSyntax:
            case YieldStatementSyntax:
            case ReturnStatementSyntax:
            case ExitStatementSyntax:
            case PostfixUnaryStatementSyntax:
            case IfStatementSyntax:
            case WhileStatementSyntax:
            case ForStatementSyntax:
            case DoWhileStatementSyntax:
            case BreakStatementSyntax:
            case ContinueStatementSyntax:
                return true;

            // Explicit developer labels/gotos stay as-is inside structured bodies.
            // Compiler-generated @NNN@ control flow must already be resolved.
            case GotoLabelStatementSyntax label:
                return ControlFlowLabels.TryGetLabelName(label.Label, out string? labelName) &&
                       labelName is not null &&
                       ControlFlowLabels.IsDeveloperLabel(labelName);

            case GotoStatementSyntax gotoStatement:
                return gotoStatement.Targets.Elements.All(ControlFlowLabels.IsDeveloperLabelTarget);

            case IfGotoStatementSyntax ifGoto:
                return ControlFlowLabels.IsDeveloperLabelTarget(ifGoto.Goto.Target);

            case IfNotGotoStatementSyntax ifNotGoto:
                return ControlFlowLabels.IsDeveloperLabelTarget(ifNotGoto.Goto.Target);

            case BlockSyntax:
                return false;

            default:
                return false;
        }
    }

    public static ValueExpressionSyntax RequireValueExpression(ExpressionSyntax expression)
    {
        if (expression is ValueExpressionSyntax value)
            return value;

        return new ValueExpressionSyntax(expression);
    }

    public static bool IsLiteralOne(ExpressionSyntax expression)
    {
        expression = ControlFlowLabels.UnwrapCondition(expression);
        return expression is LiteralExpressionSyntax
        {
            Literal.RawKind: (int)SyntaxTokenKind.NumericLiteral,
            Literal.Text: "1"
        };
    }
}
