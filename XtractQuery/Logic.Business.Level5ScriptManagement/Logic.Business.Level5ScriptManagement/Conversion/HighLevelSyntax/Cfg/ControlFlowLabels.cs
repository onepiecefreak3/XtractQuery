using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;

/// <summary>
/// Shared label-name helpers for CFG construction and structured raising passes.
/// </summary>
internal static class ControlFlowLabels
{
    public static bool TryGetLabelName(ValueExpressionSyntax target, out string? label)
    {
        label = null;
        return target.Value is LiteralExpressionSyntax literal && TryGetLabelName(literal, out label);
    }

    public static bool TryGetLabelName(LiteralExpressionSyntax literal, out string? label)
    {
        label = null;

        switch (literal.Literal.RawKind)
        {
            case (int)SyntaxTokenKind.StringLiteral:
                // "name"
                label = literal.Literal.Text[1..^1].Replace("\\\"", "\"");
                return true;

            case (int)SyntaxTokenKind.HashStringLiteral:
                // "name"h — developer jump targets often use hashed string literals.
                label = literal.Literal.Text[1..^2].Replace("\\\"", "\"");
                return true;

            default:
                return false;
        }
    }

    public static bool TryGetLabelDefinition(StatementSyntax statement, out string? label)
    {
        label = null;
        return statement is GotoLabelStatementSyntax labelStatement &&
               TryGetLabelName(labelStatement.Label, out label);
    }

    public static bool TryGetSingleGotoTarget(GotoStatementSyntax gotoStatement, out string? label)
    {
        label = null;
        if (gotoStatement.Targets.Elements.Count != 1)
            return false;

        return TryGetLabelName(gotoStatement.Targets.Elements[0], out label);
    }

    public static bool IsNumericJumpLabel(string label)
    {
        // "@000@", "@1234@", ... — at least 3 digits between '@'
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

    public static bool IsDeveloperLabel(string label) => !IsNumericJumpLabel(label);

    public static bool IsDeveloperLabelTarget(ValueExpressionSyntax target)
    {
        return TryGetLabelName(target, out string? name) &&
               name is not null &&
               IsDeveloperLabel(name);
    }

    public static int CountLabelReferences(IReadOnlyList<StatementSyntax> statements, string labelName)
    {
        return statements.Sum(s => CountLabelReferences(s, labelName));
    }

    public static int CountLabelReferences(StatementSyntax statement, string labelName)
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

            case ForStatementSyntax forStatement:
                return forStatement.Body.Statements.Sum(s => CountLabelReferences(s, labelName));

            case DoWhileStatementSyntax doWhile:
                return doWhile.Body.Statements.Sum(s => CountLabelReferences(s, labelName));

            case BlockSyntax block:
                return block.Statements.Sum(s => CountLabelReferences(s, labelName));

            default:
                return 0;
        }
    }

    public static int CountLabelReferencesOutsideRange(
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

    public static bool ContainsLabelReference(IReadOnlyList<StatementSyntax> statements, string labelName)
    {
        return statements.Any(s => CountLabelReferences(s, labelName) > 0);
    }

    public static bool IsLabel(ValueExpressionSyntax target, string labelName)
    {
        return TryGetLabelName(target, out string? name) && name == labelName;
    }

    public static ExpressionSyntax UnwrapCondition(ExpressionSyntax expression)
    {
        expression = ExpressionParenthesizer.UnwrapParentheses(expression);

        if (expression is ValueExpressionSyntax { MetadataParameters: null } value)
            return UnwrapCondition(value.Value);

        return expression;
    }
}
