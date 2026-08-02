using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal class StructuredIfPass(ILevel5SyntaxFactory syntaxFactory) : IStructuredIfPass
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
                if (TryMatchIfElse(
                        result,
                        i,
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

                    result.RemoveRange(i, ifElseContentLength);
                    result.Insert(i, ifElse);
                    changed = true;
                    break;
                }

                if (TryMatchIfThen(
                        result,
                        i,
                        out IfStatementSyntax? ifThen,
                        out int ifThenContentLength,
                        out int ifThenEndIndex,
                        out bool removeIfThenEnd) &&
                    ifThen is not null)
                {
                    if (removeIfThenEnd)
                        result.RemoveAt(ifThenEndIndex);

                    result.RemoveRange(i, ifThenContentLength);
                    result.Insert(i, ifThen);
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
                {
                    StatementSyntax elseStmt = RecurseStatement(ifStatement.Else.Statement);
                    ifStatement.Else.SetStatement(elseStmt, false);
                }

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

    private bool TryMatchIfElse(
        IReadOnlyList<StatementSyntax> statements,
        int index,
        out IfStatementSyntax? replacement,
        out int contentLength,
        out int joinLabelIndex,
        out bool removeJoinLabel)
    {
        replacement = null;
        contentLength = 0;
        joinLabelIndex = -1;
        removeJoinLabel = false;

        if (!TryGetBranchSkip(statements[index], out ExpressionSyntax? condition, out string? elseLabel) ||
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

        joinLabelIndex = FindLabelIndex(statements, joinLabel, elseLabelIndex + 1);
        if (joinLabelIndex < 0)
            return false;

        // Nested if/else often shares a merge instruction with an outer join, so other
        // fallthrough labels may sit between the else body and this join (e.g. "@015@":
        // before "@021@":). Those labels are not part of the else body and must stay put.
        int elseContentStart = elseLabelIndex + 1;
        int elseContentEnd = FindContentEndBeforeJoin(statements, elseContentStart, joinLabelIndex);

        var thenBody = statements.Skip(index + 1).Take(elseLabelIndex - index - 2).ToList();
        var elseBody = statements.Skip(elseContentStart).Take(elseContentEnd - elseContentStart).ToList();

        if (!IsStructuredBody(thenBody) || !IsStructuredBody(elseBody))
            return false;

        if (ContainsLabelReference(thenBody, elseLabel) || ContainsLabelReference(elseBody, elseLabel))
            return false;

        if (ContainsLabelReference(thenBody, joinLabel) || ContainsLabelReference(elseBody, joinLabel))
            return false;

        removeJoinLabel = CountLabelReferences(statements, joinLabel) == 1;

        replacement = CreateIfStatement(condition, thenBody, CreateElseClause(elseBody));
        contentLength = elseContentEnd - index;
        return true;
    }

    // Exclusive end of else-body statements, skipping trailing numeric labels that share the join instruction.
    private static int FindContentEndBeforeJoin(
        IReadOnlyList<StatementSyntax> statements,
        int contentStart,
        int joinLabelIndex)
    {
        int end = joinLabelIndex;
        while (end > contentStart && IsNumericJumpLabelDefinition(statements[end - 1]))
            end--;

        return end;
    }

    private static bool IsNumericJumpLabelDefinition(StatementSyntax statement)
    {
        return statement is GotoLabelStatementSyntax label &&
               TryGetLabelName(label.Label, out string? name) &&
               name is not null &&
               IsNumericJumpLabel(name);
    }

    private bool TryMatchIfThen(
        IReadOnlyList<StatementSyntax> statements,
        int index,
        out IfStatementSyntax? replacement,
        out int contentLength,
        out int endLabelIndex,
        out bool removeEndLabel)
    {
        replacement = null;
        contentLength = 0;
        endLabelIndex = -1;
        removeEndLabel = false;

        if (!TryGetBranchSkip(statements[index], out ExpressionSyntax? condition, out string? endLabel) ||
            condition is null || endLabel is null)
            return false;

        if (!IsNumericJumpLabel(endLabel))
            return false;

        endLabelIndex = FindLabelIndex(statements, endLabel, index + 1);
        if (endLabelIndex < 0)
            return false;

        if (CountLabelReferences(statements, endLabel) != 1)
            return false;

        // Same coalesced-join case as if/else: an outer join label may sit between the then
        // body and this end label (e.g. "@014@": before "@023@":).
        int thenContentEnd = FindContentEndBeforeJoin(statements, index + 1, endLabelIndex);
        var thenBody = statements.Skip(index + 1).Take(thenContentEnd - index - 1).ToList();
        if (!IsStructuredBody(thenBody))
            return false;

        if (ContainsLabelReference(thenBody, endLabel))
            return false;

        removeEndLabel = true;
        replacement = CreateIfStatement(condition, thenBody, elseClause: null);
        contentLength = thenContentEnd - index;
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

    private bool TryGetBranchSkip(
        StatementSyntax statement,
        out ExpressionSyntax? thenCondition,
        out string? targetLabel)
    {
        thenCondition = null;
        targetLabel = null;

        // if not X goto L; body; L:  ≡  if X { body }
        if (statement is IfNotGotoStatementSyntax ifNotGoto)
        {
            if (!TryGetLabelName(ifNotGoto.Goto.Target, out targetLabel) || targetLabel is null)
                return false;

            thenCondition = UnwrapCondition(ifNotGoto.Comparison.Value);
            return true;
        }

        // if X goto L; body; L:  ≡  if not X { body }
        // (EmitIfNotGoto peels a leading not into this form.)
        if (statement is IfGotoStatementSyntax ifGoto)
        {
            if (!TryGetLabelName(ifGoto.Goto.Target, out targetLabel) || targetLabel is null)
                return false;

            thenCondition = CreateNotCondition(UnwrapCondition(ifGoto.Value));
            return true;
        }

        return false;
    }

    private ExpressionSyntax CreateNotCondition(ExpressionSyntax expression)
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

            case WhileStatementSyntax { Body: not null } whileStatement:
                return CountLabelReferencesInBlock(whileStatement.Body, labelName);

            case DoWhileStatementSyntax doWhile:
                return CountLabelReferencesInBlock(doWhile.Body, labelName);

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
            case WhileStatementSyntax:
            case DoWhileStatementSyntax:
            case BreakStatementSyntax:
            case ContinueStatementSyntax:
                return true;

            // Explicit developer labels/gotos stay as-is inside structured bodies.
            // Compiler-generated @NNN@ control flow must already be resolved.
            case GotoLabelStatementSyntax label:
                return TryGetLabelName(label.Label, out string? labelName) &&
                       labelName is not null &&
                       !IsNumericJumpLabel(labelName);

            case GotoStatementSyntax gotoStatement:
                return gotoStatement.Targets.Elements.All(IsDeveloperLabelTarget);

            case IfGotoStatementSyntax ifGoto:
                return IsDeveloperLabelTarget(ifGoto.Goto.Target);

            case IfNotGotoStatementSyntax ifNotGoto:
                return IsDeveloperLabelTarget(ifNotGoto.Goto.Target);

            case BlockSyntax:
                return false;

            default:
                return false;
        }
    }

    private static bool IsDeveloperLabelTarget(ValueExpressionSyntax target)
    {
        return TryGetLabelName(target, out string? name) &&
               name is not null &&
               !IsNumericJumpLabel(name);
    }
}
