using Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax.Cfg;
using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

/// <summary>
/// Raises classic counted <c>while</c> loops into <c>for</c> after structured
/// loop/if raising. Matches <c>init; while (cond) { body; step; }</c> where
/// <c>step</c> is a forward induction on a variable that <c>cond</c> also
/// references (e.g. <c>i++</c> / <c>i += n</c> / <c>i = i + n</c>).
/// Rejects <c>while (true)</c> scanners and countdown waits whose trailing
/// update is not a forward induction — those stay as <c>while</c>.
/// Break/continue stay as keywords inside the for body.
/// </summary>
internal class StructuredForPass(ILevel5SyntaxFactory syntaxFactory) : IStructuredForPass
{
    public IReadOnlyList<StatementSyntax> Apply(IReadOnlyList<StatementSyntax> statements)
    {
        return StructuredSyntaxRecursor.Apply(statements, ApplyFlat, syntaxFactory);
    }

    private IReadOnlyList<StatementSyntax> ApplyFlat(IReadOnlyList<StatementSyntax> statements)
    {
        var result = new List<StatementSyntax>();
        for (var i = 0; i < statements.Count; i++)
        {
            if (statements[i] is WhileStatementSyntax { Body: not null } whileStatement &&
                TryRaiseFor(statements, i, whileStatement, out ForStatementSyntax? forStatement, out bool consumedInit) &&
                forStatement is not null)
            {
                if (consumedInit)
                    result.RemoveAt(result.Count - 1);

                result.Add(forStatement);
                continue;
            }

            result.Add(statements[i]);
        }

        return result;
    }

    private bool TryRaiseFor(
        IReadOnlyList<StatementSyntax> statements,
        int whileIndex,
        WhileStatementSyntax whileStatement,
        out ForStatementSyntax? replacement,
        out bool consumedInit)
    {
        replacement = null;
        consumedInit = false;

        IReadOnlyList<StatementSyntax> body = whileStatement.Body!.Statements;
        if (body.Count == 0)
            return false;

        StatementSyntax step = body[^1];
        if (!TryGetUpdatedVariable(step, out string? variable) || variable is null)
            return false;

        // while (true) { …; i++; } and similar: step alone does not make a counted for.
        if (!ReferencesVariable(whileStatement.Condition, variable))
            return false;

        // Countdown waits (i -= dt / i--) are while-shaped, not classic counted fors.
        if (!IsForwardInductionStep(step, variable))
            return false;

        // For-lowering may leave a continue latch label immediately before the step.
        // Strip it (and rewrite jumps to it as continue) so the latch does not dangle.
        IReadOnlyList<StatementSyntax> bodyWithoutStep = PeelContinueLatch(body.Take(body.Count - 1).ToList());

        StatementSyntax? initializer = null;
        if (whileIndex > 0 &&
            TryGetAssignedVariable(statements[whileIndex - 1], out string? initVariable) &&
            initVariable == variable)
        {
            initializer = statements[whileIndex - 1];
            consumedInit = true;
        }

        StatementSyntax iterator = CreateIteratorClause(step);

        replacement = CreateFor(initializer, whileStatement.Condition, iterator, bodyWithoutStep);
        return true;
    }

    private IReadOnlyList<StatementSyntax> PeelContinueLatch(List<StatementSyntax> bodyWithoutStep)
    {
        if (bodyWithoutStep.Count == 0)
            return bodyWithoutStep;

        if (bodyWithoutStep[^1] is not GotoLabelStatementSyntax labelStatement ||
            !ControlFlowLabels.TryGetLabelDefinition(labelStatement, out string? latchLabel) ||
            latchLabel is null ||
            !ControlFlowLabels.IsNumericJumpLabel(latchLabel))
        {
            return bodyWithoutStep;
        }

        bodyWithoutStep.RemoveAt(bodyWithoutStep.Count - 1);
        return RewriteContinuesToLatch(bodyWithoutStep, latchLabel);
    }

    private IReadOnlyList<StatementSyntax> RewriteContinuesToLatch(
        IReadOnlyList<StatementSyntax> statements,
        string latchLabel)
    {
        var result = new List<StatementSyntax>(statements.Count);
        foreach (StatementSyntax statement in statements)
            result.Add(RewriteContinueToLatch(statement, latchLabel));
        return result;
    }

    private StatementSyntax RewriteContinueToLatch(StatementSyntax statement, string latchLabel)
    {
        switch (statement)
        {
            case GotoStatementSyntax gotoStatement
                when ControlFlowLabels.TryGetSingleGotoTarget(gotoStatement, out string? target) &&
                     target == latchLabel:
                return CreateContinue();

            case IfStatementSyntax ifStatement:
            {
                IReadOnlyList<StatementSyntax> thenBody = RewriteContinuesToLatch(ifStatement.Body.Statements, latchLabel);
                ifStatement.SetBody(StructuredSyntaxRecursor.CreateBlock(thenBody, syntaxFactory), false);
                if (ifStatement.Else != null)
                    ifStatement.Else.SetStatement(RewriteContinueToLatch(ifStatement.Else.Statement, latchLabel), false);
                return ifStatement;
            }

            case BlockSyntax block:
            {
                IReadOnlyList<StatementSyntax> nested = RewriteContinuesToLatch(block.Statements, latchLabel);
                block.SetStatements(nested, false);
                return block;
            }

            // Nested loops keep their own continue targets.
            default:
                return statement;
        }
    }

    private ContinueStatementSyntax CreateContinue()
    {
        return new ContinueStatementSyntax(
            syntaxFactory.Token(SyntaxTokenKind.ContinueKeyword),
            syntaxFactory.Token(SyntaxTokenKind.Semicolon));
    }

    private ForStatementSyntax CreateFor(
        StatementSyntax? initializer,
        ExpressionSyntax condition,
        StatementSyntax iterator,
        IReadOnlyList<StatementSyntax> body)
    {
        SyntaxToken? firstSemicolon = null;
        if (initializer is null)
            firstSemicolon = syntaxFactory.Token(SyntaxTokenKind.Semicolon);

        return new ForStatementSyntax(
            syntaxFactory.Token(SyntaxTokenKind.ForKeyword),
            syntaxFactory.Token(SyntaxTokenKind.ParenOpen),
            initializer,
            firstSemicolon,
            condition,
            syntaxFactory.Token(SyntaxTokenKind.Semicolon),
            iterator,
            syntaxFactory.Token(SyntaxTokenKind.ParenClose),
            StructuredSyntaxRecursor.CreateBlock(body, syntaxFactory));
    }

    private StatementSyntax CreateIteratorClause(StatementSyntax step)
    {
        SyntaxToken emptySemicolon = new(string.Empty, (int)SyntaxTokenKind.Semicolon);

        switch (step)
        {
            case AssignmentStatementSyntax assignment:
                return new AssignmentStatementSyntax(
                    assignment.Left,
                    assignment.EqualsOperator,
                    assignment.Right,
                    emptySemicolon);

            case PostfixUnaryStatementSyntax postfix:
                return new PostfixUnaryStatementSyntax(postfix.Expression, emptySemicolon);

            default:
                return step;
        }
    }

    /// <summary>
    /// Any write to a plain variable (for initializer folding).
    /// </summary>
    private static bool TryGetAssignedVariable(StatementSyntax statement, out string? variable)
    {
        variable = null;

        switch (statement)
        {
            case PostfixUnaryStatementSyntax postfix:
                return TryGetVariableName(postfix.Expression.Value, out variable);

            case AssignmentStatementSyntax assignment:
                return TryGetVariableName(assignment.Left, out variable);

            default:
                return false;
        }
    }

    /// <summary>
    /// Induction-style update used as a for iterator: ++/-- , compound assign, or
    /// <c>$v = …$v…</c>.
    /// </summary>
    private static bool TryGetUpdatedVariable(StatementSyntax statement, out string? variable)
    {
        variable = null;

        switch (statement)
        {
            case PostfixUnaryStatementSyntax postfix:
                return TryGetVariableName(postfix.Expression.Value, out variable);

            case AssignmentStatementSyntax assignment:
            {
                if (!TryGetVariableName(assignment.Left, out variable) || variable is null)
                    return false;

                if (assignment.EqualsOperator.RawKind != (int)SyntaxTokenKind.EqualsSign)
                    return true;

                return ReferencesVariable(assignment.Right, variable);
            }

            default:
                return false;
        }
    }

    /// <summary>
    /// Classic counted-for step: <c>++</c>, <c>+=</c>, or <c>$v = $v + …</c> /
    /// <c>$v = … + $v</c>. Decrements and other updates stay on <c>while</c>.
    /// </summary>
    private static bool IsForwardInductionStep(StatementSyntax step, string variable)
    {
        switch (step)
        {
            case PostfixUnaryStatementSyntax postfix:
                return postfix.Expression.Operation.RawKind == (int)SyntaxTokenKind.Increment &&
                       TryGetVariableName(postfix.Expression.Value, out string? postfixVariable) &&
                       postfixVariable == variable;

            case AssignmentStatementSyntax assignment:
            {
                if (!TryGetVariableName(assignment.Left, out string? left) || left != variable)
                    return false;

                if (assignment.EqualsOperator.RawKind == (int)SyntaxTokenKind.PlusEquals)
                    return true;

                if (assignment.EqualsOperator.RawKind != (int)SyntaxTokenKind.EqualsSign)
                    return false;

                return IsAdditionUpdatingVariable(assignment.Right, variable);
            }

            default:
                return false;
        }
    }

    private static bool IsAdditionUpdatingVariable(ExpressionSyntax expression, string variable)
    {
        expression = Unwrap(expression);

        if (expression is ValueExpressionSyntax value)
            return IsAdditionUpdatingVariable(value.Value, variable);

        if (expression is not BinaryExpressionSyntax
            {
                Operation.RawKind: (int)SyntaxTokenKind.Plus
            } binary)
        {
            return false;
        }

        bool leftIsVariable = IsVariableNamed(binary.Left, variable);
        bool rightIsVariable = IsVariableNamed(binary.Right, variable);
        return leftIsVariable ^ rightIsVariable;
    }

    private static bool IsVariableNamed(ExpressionSyntax expression, string variable)
    {
        return TryGetVariableName(expression, out string? name) && name == variable;
    }

    private static bool TryGetVariableName(ExpressionSyntax expression, out string? variable)
    {
        variable = null;
        expression = Unwrap(expression);

        if (expression is ValueExpressionSyntax { Value: VariableExpressionSyntax valueVariable })
        {
            variable = valueVariable.Variable.Text;
            return true;
        }

        if (expression is VariableExpressionSyntax variableExpression)
        {
            variable = variableExpression.Variable.Text;
            return true;
        }

        return false;
    }

    private static bool ReferencesVariable(ExpressionSyntax expression, string variable)
    {
        expression = Unwrap(expression);

        switch (expression)
        {
            case ValueExpressionSyntax value:
                return ReferencesVariable(value.Value, variable);

            case VariableExpressionSyntax variableExpression:
                return variableExpression.Variable.Text == variable;

            case UnaryExpressionSyntax unary:
                return ReferencesVariable(unary.Value, variable);

            case BinaryExpressionSyntax binary:
                return ReferencesVariable(binary.Left, variable) || ReferencesVariable(binary.Right, variable);

            case LogicalExpressionSyntax logical:
                return ReferencesVariable(logical.Left, variable) || ReferencesVariable(logical.Right, variable);

            case AssignmentExpressionSyntax assignment:
                return ReferencesVariable(assignment.Left, variable) || ReferencesVariable(assignment.Right, variable);

            case ParenthesizedExpressionSyntax parenthesized:
                return ReferencesVariable(parenthesized.Expression, variable);

            case TypeCastValueExpressionSyntax typeCast:
                return ReferencesVariable(typeCast.Value, variable);

            case ArrayIndexExpressionSyntax arrayIndex:
                if (ReferencesVariable(arrayIndex.Value, variable))
                    return true;
                return arrayIndex.Indexer.Any(index => ReferencesVariable(index.Index, variable));

            case MethodInvocationExpressionSyntax invocation:
                if (invocation.Parameters.ParameterList is null)
                    return false;
                return invocation.Parameters.ParameterList.Elements.Any(parameter =>
                    ReferencesVariable(parameter, variable));

            default:
                return false;
        }
    }

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
            expression = parenthesized.Expression;

        return expression;
    }
}
