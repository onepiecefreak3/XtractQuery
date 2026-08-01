using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal class StructuredIfPass(ILevel5SyntaxFactory syntaxFactory) : IStructuredIfPass
{
    public IReadOnlyList<StatementSyntax> Apply(IReadOnlyList<StatementSyntax> statements)
    {
        var result = statements.ToList();
        var changed = true;

        while (changed)
        {
            changed = false;

            for (var i = 0; i < result.Count; i++)
            {
                if (TryMatchIfElse(result, i, out IfStatementSyntax? ifElse, out int ifElseLength) && ifElse is not null)
                {
                    result.RemoveRange(i, ifElseLength);
                    result.Insert(i, ifElse);
                    changed = true;
                    break;
                }

                if (TryMatchIfThen(result, i, out IfStatementSyntax? ifThen, out int ifThenLength) && ifThen is not null)
                {
                    result.RemoveRange(i, ifThenLength);
                    result.Insert(i, ifThen);
                    changed = true;
                    break;
                }
            }
        }

        return result;
    }

    private bool TryMatchIfElse(
        IReadOnlyList<StatementSyntax> statements,
        int index,
        out IfStatementSyntax? replacement,
        out int length)
    {
        replacement = null;
        length = 0;

        if (!TryGetIfNotGoto(statements[index], out ExpressionSyntax? condition, out string? elseLabel) ||
            condition is null || elseLabel is null)
            return false;

        if (!IsNumericJumpLabel(elseLabel))
            return false;

        int elseLabelIndex = FindLabelIndex(statements, elseLabel, index + 1);
        if (elseLabelIndex < 0)
            return false;

        if (CountLabelReferences(statements, elseLabel) != 1)
            return false;

        if (elseLabelIndex - 1 <= index)
            return false;

        if (statements[elseLabelIndex - 1] is not GotoStatementSyntax joinGoto ||
            !TryGetSingleGotoTarget(joinGoto, out string? joinLabel) ||
            joinLabel is null)
            return false;

        if (!IsNumericJumpLabel(joinLabel))
            return false;

        int joinLabelIndex = FindLabelIndex(statements, joinLabel, elseLabelIndex + 1);
        if (joinLabelIndex < 0)
            return false;

        var thenBody = statements.Skip(index + 1).Take(elseLabelIndex - index - 2).ToList();
        var elseBody = statements.Skip(elseLabelIndex + 1).Take(joinLabelIndex - elseLabelIndex - 1).ToList();

        if (!IsStructuredBody(thenBody) || !IsStructuredBody(elseBody))
            return false;

        if (ContainsLabelReference(thenBody, elseLabel) || ContainsLabelReference(elseBody, elseLabel))
            return false;

        if (ContainsLabelReference(thenBody, joinLabel) || ContainsLabelReference(elseBody, joinLabel))
            return false;

        bool removeJoinLabel = CountLabelReferences(statements, joinLabel) == 1;

        replacement = CreateIfStatement(condition, thenBody, CreateElseClause(elseBody));
        length = joinLabelIndex - index + (removeJoinLabel ? 1 : 0);
        return true;
    }

    private bool TryMatchIfThen(
        IReadOnlyList<StatementSyntax> statements,
        int index,
        out IfStatementSyntax? replacement,
        out int length)
    {
        replacement = null;
        length = 0;

        if (!TryGetIfNotGoto(statements[index], out ExpressionSyntax? condition, out string? endLabel) ||
            condition is null || endLabel is null)
            return false;

        if (!IsNumericJumpLabel(endLabel))
            return false;

        int endLabelIndex = FindLabelIndex(statements, endLabel, index + 1);
        if (endLabelIndex < 0)
            return false;

        if (CountLabelReferences(statements, endLabel) != 1)
            return false;

        var thenBody = statements.Skip(index + 1).Take(endLabelIndex - index - 1).ToList();
        if (!IsStructuredBody(thenBody))
            return false;

        if (ContainsLabelReference(thenBody, endLabel))
            return false;

        bool removeEndLabel = true;
        replacement = CreateIfStatement(condition, thenBody, elseClause: null);
        length = endLabelIndex - index + (removeEndLabel ? 1 : 0);
        return true;
    }

    private ElseClauseSyntax CreateElseClause(IReadOnlyList<StatementSyntax> elseBody)
    {
        // Collapse a single nested if into `else if`.
        if (elseBody.Count == 1 && elseBody[0] is IfStatementSyntax nestedIf)
            return new ElseClauseSyntax(syntaxFactory.Token(SyntaxTokenKind.ElseKeyword), nestedIf);

        return new ElseClauseSyntax(syntaxFactory.Token(SyntaxTokenKind.ElseKeyword), CreateBlock(elseBody));
    }

    private IfStatementSyntax CreateIfStatement(
        ExpressionSyntax condition,
        IReadOnlyList<StatementSyntax> thenBody,
        ElseClauseSyntax? elseClause)
    {
        return new IfStatementSyntax(
            syntaxFactory.Token(SyntaxTokenKind.IfKeyword),
            condition,
            CreateBlock(thenBody),
            elseClause);
    }

    private BlockSyntax CreateBlock(IReadOnlyList<StatementSyntax> statements)
    {
        return new BlockSyntax(
            syntaxFactory.Token(SyntaxTokenKind.CurlyOpen),
            statements,
            syntaxFactory.Token(SyntaxTokenKind.CurlyClose));
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
            if (statements[i] is GotoLabelStatementSyntax labelStatement &&
                TryGetLabelName(labelStatement.Label, out string? name) &&
                name == labelName)
                return i;
        }

        return -1;
    }

    private static int CountLabelReferences(IReadOnlyList<StatementSyntax> statements, string labelName)
    {
        var count = 0;

        foreach (StatementSyntax statement in statements)
            count += CountLabelReferences(statement, labelName);

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
                var count = CountLabelReferencesInBlock(ifStatement.Body, labelName);
                if (ifStatement.Else != null)
                    count += CountLabelReferences(ifStatement.Else.Statement, labelName);
                return count;

            case BlockSyntax block:
                return CountLabelReferencesInBlock(block, labelName);

            default:
                return 0;
        }
    }

    private static int CountLabelReferencesInBlock(BlockSyntax block, string labelName)
    {
        return block.Statements.Sum(s => CountLabelReferences(s, labelName));
    }

    private static bool ContainsLabelReference(IReadOnlyList<StatementSyntax> statements, string labelName)
    {
        return statements.Any(s => CountLabelReferences(s, labelName) > 0);
    }

    private static bool IsLabel(ValueExpressionSyntax target, string labelName)
    {
        return TryGetLabelName(target, out string? name) && name == labelName;
    }

    private static bool IsNumericJumpLabel(string label)
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

    private static bool IsStructuredBody(IReadOnlyList<StatementSyntax> statements)
    {
        return statements.All(IsStructuredBodyStatement);
    }

    private static bool IsStructuredBodyStatement(StatementSyntax statement)
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
                return true;

            case BlockSyntax:
                return false;

            default:
                return false;
        }
    }
}
