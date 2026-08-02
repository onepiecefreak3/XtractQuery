using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal class StructuredLoopPass(ILevel5SyntaxFactory syntaxFactory) : IStructuredLoopPass
{
    public IReadOnlyList<StatementSyntax> Apply(IReadOnlyList<StatementSyntax> statements)
    {
        var result = ApplyFlat(statements);
        return RecurseIntoNested(result);
    }

    private IReadOnlyList<StatementSyntax> ApplyFlat(IReadOnlyList<StatementSyntax> statements)
    {
        var result = statements.ToList();
        var changed = true;

        while (changed)
        {
            changed = false;

            for (var i = 0; i < result.Count; i++)
            {
                if (TryMatchSpinLoop(result, i, out WhileStatementSyntax? spin, out int spinLength) && spin is not null)
                {
                    result.RemoveRange(i, spinLength);
                    result.Insert(i, spin);
                    changed = true;
                    break;
                }

                if (TryMatchTopTestedWhile(result, i, out WhileStatementSyntax? topWhile, out int topLength) && topWhile is not null)
                {
                    result.RemoveRange(i, topLength);
                    result.Insert(i, topWhile);
                    changed = true;
                    break;
                }

                if (TryMatchDoWhile(result, i, out DoWhileStatementSyntax? doWhile, out int doLength) && doWhile is not null)
                {
                    result.RemoveRange(i, doLength);
                    result.Insert(i, doWhile);
                    changed = true;
                    break;
                }
            }
        }

        return result;
    }

    private IReadOnlyList<StatementSyntax> RecurseIntoNested(IReadOnlyList<StatementSyntax> statements)
    {
        var result = new List<StatementSyntax>(statements.Count);
        foreach (StatementSyntax statement in statements)
            result.Add(RecurseStatement(statement));
        return result;
    }

    private StatementSyntax RecurseStatement(StatementSyntax statement)
    {
        switch (statement)
        {
            case WhileStatementSyntax { Body: not null } whileStatement:
            {
                IReadOnlyList<StatementSyntax> body = Apply(whileStatement.Body.Statements);
                whileStatement.SetBody(CreateBlock(body), false);
                return whileStatement;
            }

            case DoWhileStatementSyntax doWhile:
            {
                IReadOnlyList<StatementSyntax> body = Apply(doWhile.Body.Statements);
                doWhile.SetBody(CreateBlock(body), false);
                return doWhile;
            }

            case IfStatementSyntax ifStatement:
            {
                IReadOnlyList<StatementSyntax> thenBody = Apply(ifStatement.Body.Statements);
                ifStatement.SetBody(CreateBlock(thenBody), false);
                if (ifStatement.Else != null)
                    ifStatement.SetElse(RecurseElse(ifStatement.Else), false);
                return ifStatement;
            }

            case BlockSyntax block:
            {
                IReadOnlyList<StatementSyntax> nested = Apply(block.Statements);
                block.SetStatements(nested, false);
                return block;
            }

            default:
                return statement;
        }
    }

    private ElseClauseSyntax RecurseElse(ElseClauseSyntax elseClause)
    {
        StatementSyntax nested = RecurseStatement(elseClause.Statement);
        elseClause.SetStatement(nested, false);
        return elseClause;
    }

    private bool TryMatchSpinLoop(
        IReadOnlyList<StatementSyntax> statements,
        int index,
        out WhileStatementSyntax? replacement,
        out int length)
    {
        replacement = null;
        length = 0;

        if (index + 1 >= statements.Count)
            return false;

        if (!TryGetLabel(statements[index], out string? headLabel) || headLabel is null)
            return false;

        if (!IsNumericJumpLabel(headLabel))
            return false;

        ExpressionSyntax? condition = null;

        if (statements[index + 1] is IfGotoStatementSyntax ifGoto &&
            TryGetLabelName(ifGoto.Goto.Target, out string? target) &&
            target == headLabel)
        {
            condition = UnwrapCondition(ifGoto.Value);
        }
        else if (statements[index + 1] is IfNotGotoStatementSyntax ifNotGoto &&
                 TryGetLabelName(ifNotGoto.Goto.Target, out string? notTarget) &&
                 notTarget == headLabel)
        {
            condition = ifNotGoto.Comparison;
        }
        else
            return false;

        if (CountLabelReferences(statements, headLabel) != 1)
            return false;

        replacement = CreateWhileOneLiner(condition);
        length = 2;
        return true;
    }

    private bool TryMatchTopTestedWhile(
        IReadOnlyList<StatementSyntax> statements,
        int index,
        out WhileStatementSyntax? replacement,
        out int length)
    {
        replacement = null;
        length = 0;

        if (!TryGetLabel(statements[index], out string? headLabel) || headLabel is null)
            return false;

        if (!IsNumericJumpLabel(headLabel))
            return false;

        if (index + 1 >= statements.Count)
            return false;

        ExpressionSyntax? condition;
        string? exitLabel;

        if (TryGetIfNotGoto(statements[index + 1], out ExpressionSyntax? positiveCondition, out exitLabel) &&
            positiveCondition is not null && exitLabel is not null)
        {
            condition = positiveCondition;
        }
        else if (statements[index + 1] is IfGotoStatementSyntax ifGoto &&
                 TryGetLabelName(ifGoto.Goto.Target, out exitLabel) &&
                 exitLabel is not null)
        {
            // L: if cond goto EXIT; body; goto L; EXIT:  ≡  while (not cond) { body }
            condition = new UnaryExpressionSyntax(
                syntaxFactory.Token(SyntaxTokenKind.NotKeyword),
                RequireValueExpression(UnwrapCondition(ifGoto.Value)));
        }
        else
            return false;

        if (!IsNumericJumpLabel(exitLabel))
            return false;

        int exitLabelIndex = FindLabelIndex(statements, exitLabel, index + 2);
        if (exitLabelIndex < 0)
            return false;

        if (CountLabelReferences(statements, exitLabel) < 1)
            return false;

        var rawBody = statements.Skip(index + 2).Take(exitLabelIndex - index - 2).ToList();
        if (rawBody.Count == 0)
            return false;

        if (rawBody[^1] is not GotoStatementSyntax backEdge ||
            !TryGetSingleGotoTarget(backEdge, out string? backTarget) ||
            backTarget != headLabel)
            return false;

        var bodyWithoutBackEdge = rawBody.Take(rawBody.Count - 1).ToList();
        if (!IsValidLoopBody(bodyWithoutBackEdge, headLabel, exitLabel, statements, index, exitLabelIndex))
            return false;

        // Head may only be targeted by the trailing back-edge and continues inside the body.
        int headRefsOutsideBody = CountLabelReferencesOutsideRange(statements, headLabel, index + 2, exitLabelIndex);
        if (headRefsOutsideBody != 0)
            return false;

        IReadOnlyList<StatementSyntax> rewrittenBody = RewriteLoopBody(bodyWithoutBackEdge, headLabel, exitLabel);
        ExpressionSyntax whileCondition = IsLiteralOne(condition) ? CreateTrueLiteral() : condition;

        int exitRefsInBody = CountLabelReferencesInStatements(bodyWithoutBackEdge, exitLabel);
        int exitRefsTotal = CountLabelReferences(statements, exitLabel);
        // Header if-not contributes 1; body contributes breaks. After rewrite those become break.
        bool removeExitLabel = exitRefsTotal == exitRefsInBody + 1;

        replacement = CreateWhile(whileCondition, rewrittenBody);
        length = exitLabelIndex - index + (removeExitLabel ? 1 : 0);
        return true;
    }

    private bool TryMatchDoWhile(
        IReadOnlyList<StatementSyntax> statements,
        int index,
        out DoWhileStatementSyntax? replacement,
        out int length)
    {
        replacement = null;
        length = 0;

        if (!TryGetLabel(statements[index], out string? headLabel) || headLabel is null)
            return false;

        if (!IsNumericJumpLabel(headLabel))
            return false;

        // Find a trailing if-goto back to head with only structured body between.
        for (int end = index + 2; end < statements.Count; end++)
        {
            if (statements[end] is not IfGotoStatementSyntax ifGoto ||
                !TryGetLabelName(ifGoto.Goto.Target, out string? target) ||
                target != headLabel)
                continue;

            var body = statements.Skip(index + 1).Take(end - index - 1).ToList();
            if (body.Count == 0)
                continue;

            if (!IsValidLoopBody(body, headLabel, exitLabel: null, statements, index, end))
                continue;

            if (CountLabelReferencesOutsideRange(statements, headLabel, index + 1, end + 1) != 0)
                continue;

            // Exactly one reference: this if-goto (continues inside body would be more).
            int headRefs = CountLabelReferences(statements, headLabel);
            int bodyContinues = CountLabelReferencesInStatements(body, headLabel);
            if (headRefs != bodyContinues + 1)
                continue;

            IReadOnlyList<StatementSyntax> rewritten = RewriteLoopBody(body, headLabel, exitLabel: null);
            replacement = CreateDoWhile(UnwrapCondition(ifGoto.Value), rewritten);
            length = end - index + 1;
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
        HashSet<string> internalLabels = CollectDefinedLabels(body);

        foreach (StatementSyntax statement in body)
        {
            if (!AreJumpTargetsAllowed(statement, headLabel, exitLabel, internalLabels))
                return false;
        }

        // No external jump into the middle of the body (other than falling into head).
        foreach (string label in internalLabels)
        {
            if (CountLabelReferencesOutsideRange(allStatements, label, loopStart, loopEnd + 1) != 0)
                return false;
        }

        return true;
    }

    private static HashSet<string> CollectDefinedLabels(IReadOnlyList<StatementSyntax> statements)
    {
        var labels = new HashSet<string>(StringComparer.Ordinal);
        foreach (StatementSyntax statement in statements)
        {
            if (TryGetLabel(statement, out string? name) && name is not null)
                labels.Add(name);
        }

        return labels;
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
                return gotoStatement.Targets.Elements.All(t => IsAllowedTarget(t, headLabel, exitLabel, internalLabels));

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
        if (!TryGetLabelName(target, out string? name) || name is null)
            return false;

        if (name == headLabel)
            return true;

        if (exitLabel is not null && name == exitLabel)
            return true;

        return internalLabels.Contains(name);
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
            case GotoStatementSyntax gotoStatement when TryGetSingleGotoTarget(gotoStatement, out string? target):
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
            CreateBlock(body),
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
            CreateBlock(body),
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

    private BlockSyntax CreateBlock(IReadOnlyList<StatementSyntax> statements)
    {
        return new BlockSyntax(
            syntaxFactory.Token(SyntaxTokenKind.CurlyOpen),
            statements,
            syntaxFactory.Token(SyntaxTokenKind.CurlyClose));
    }

    private static ValueExpressionSyntax RequireValueExpression(ExpressionSyntax expression)
    {
        if (expression is ValueExpressionSyntax value)
            return value;

        return new ValueExpressionSyntax(expression);
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

        if (!TryGetLabelName(ifNotGoto.Goto.Target, out targetLabel) || targetLabel is null)
            return false;

        positiveCondition = UnwrapCondition(ifNotGoto.Comparison.Value);
        return true;
    }

    private static ExpressionSyntax UnwrapCondition(ExpressionSyntax expression)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);

        if (expression is ValueExpressionSyntax { MetadataParameters: null } value)
            return UnwrapCondition(value.Value);

        return expression;
    }

    private static bool IsLiteralOne(ExpressionSyntax expression)
    {
        expression = UnwrapCondition(expression);
        return expression is LiteralExpressionSyntax
        {
            Literal.RawKind: (int)SyntaxTokenKind.NumericLiteral,
            Literal.Text: "1"
        };
    }

    private static bool TryGetLabel(StatementSyntax statement, out string? label)
    {
        label = null;
        if (statement is not GotoLabelStatementSyntax labelStatement)
            return false;

        return TryGetLabelName(labelStatement.Label, out label);
    }

    private static bool TryGetSingleGotoTarget(GotoStatementSyntax gotoStatement, out string? label)
    {
        label = null;
        if (gotoStatement.Targets.Elements.Count != 1)
            return false;

        return TryGetLabelName(gotoStatement.Targets.Elements[0], out label);
    }

    private static bool TryGetLabelName(ValueExpressionSyntax target, out string? label)
    {
        label = null;
        if (target.Value is not LiteralExpressionSyntax literal)
            return false;

        return TryGetLabelName(literal, out label);
    }

    private static bool TryGetLabelName(LiteralExpressionSyntax literal, out string? label)
    {
        label = null;
        if (literal.Literal.RawKind != (int)SyntaxTokenKind.StringLiteral)
            return false;

        label = literal.Literal.Text[1..^1].Replace("\\\"", "\"");
        return true;
    }

    private static int FindLabelIndex(IReadOnlyList<StatementSyntax> statements, string labelName, int startIndex)
    {
        for (int i = startIndex; i < statements.Count; i++)
        {
            if (TryGetLabel(statements[i], out string? name) && name == labelName)
                return i;
        }

        return -1;
    }

    private static int CountLabelReferences(IReadOnlyList<StatementSyntax> statements, string labelName)
    {
        return statements.Sum(s => CountLabelReferences(s, labelName));
    }

    private static int CountLabelReferencesInStatements(IReadOnlyList<StatementSyntax> statements, string labelName)
    {
        return CountLabelReferences(statements, labelName);
    }

    private static int CountLabelReferencesOutsideRange(
        IReadOnlyList<StatementSyntax> statements,
        string labelName,
        int rangeStart,
        int rangeEndExclusive)
    {
        var count = 0;
        for (var i = 0; i < statements.Count; i++)
        {
            if (i >= rangeStart && i < rangeEndExclusive)
                continue;

            count += CountLabelReferences(statements[i], labelName);
        }

        return count;
    }

    private static int CountLabelReferences(StatementSyntax statement, string labelName)
    {
        switch (statement)
        {
            case IfNotGotoStatementSyntax ifNotGoto:
                return IsLabel(ifNotGoto.Goto.Target, labelName) ? 1 : 0;

            case IfGotoStatementSyntax ifGoto:
                return IsLabel(ifGoto.Goto.Target, labelName) ? 1 : 0;

            case GotoStatementSyntax gotoStatement:
                return gotoStatement.Targets.Elements.Count(t => IsLabel(t, labelName));

            case IfStatementSyntax ifStatement:
                var count = ifStatement.Body.Statements.Sum(s => CountLabelReferences(s, labelName));
                if (ifStatement.Else != null)
                    count += CountLabelReferences(ifStatement.Else.Statement, labelName);
                return count;

            case WhileStatementSyntax { Body: not null } whileStatement:
                return whileStatement.Body.Statements.Sum(s => CountLabelReferences(s, labelName));

            case DoWhileStatementSyntax doWhile:
                return doWhile.Body.Statements.Sum(s => CountLabelReferences(s, labelName));

            case BlockSyntax block:
                return block.Statements.Sum(s => CountLabelReferences(s, labelName));

            default:
                return 0;
        }
    }

    private static bool IsLabel(ValueExpressionSyntax target, string labelName)
    {
        return TryGetLabelName(target, out string? name) && name == labelName;
    }

    private static bool IsNumericJumpLabel(string label)
    {
        if (label.Length < 5 || label[0] != '@' || label[^1] != '@')
            return false;

        ReadOnlySpan<char> digits = label.AsSpan(1, label.Length - 2);
        if (digits.Length < 3)
            return false;

        foreach (char c in digits)
        {
            if (c is < '0' or > '9')
                return false;
        }

        return true;
    }
}
