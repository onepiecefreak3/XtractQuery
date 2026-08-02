using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal static class ExpressionSideEffectClassifier
{
    public static bool IsEffectful(StatementSyntax statement)
    {
        switch (statement)
        {
            case YieldStatementSyntax:
            case ExitStatementSyntax:
            case MethodInvocationStatementSyntax:
            case PostfixUnaryStatementSyntax:
            case BreakStatementSyntax:
            case ContinueStatementSyntax:
                return true;

            case AssignmentStatementSyntax assignment:
                return IsEffectful(assignment.Right) || assignment.EqualsOperator.RawKind != (int)SyntaxTokenKind.EqualsSign;

            case IfGotoStatementSyntax ifGoto:
                return IsEffectful(ifGoto.Value);

            case IfNotGotoStatementSyntax ifNotGoto:
                return IsEffectful(ifNotGoto.Comparison);

            case ReturnStatementSyntax { ValueExpression: not null } returnStatement:
                return IsEffectful(returnStatement.ValueExpression);

            case GotoStatementSyntax gotoStatement:
                if (gotoStatement.Targets?.Elements is null)
                    return false;
                return gotoStatement.Targets.Elements.Any(IsEffectful);

            case IfStatementSyntax ifStatement:
                if (IsEffectful(ifStatement.Condition))
                    return true;
                if (ifStatement.Body.Statements.Any(IsEffectful))
                    return true;
                return ifStatement.Else != null && IsEffectful(ifStatement.Else.Statement);

            case WhileStatementSyntax whileStatement:
                if (IsEffectful(whileStatement.Condition))
                    return true;
                return whileStatement.Body != null && whileStatement.Body.Statements.Any(IsEffectful);

            case DoWhileStatementSyntax doWhile:
                return IsEffectful(doWhile.Condition) || doWhile.Body.Statements.Any(IsEffectful);

            case BlockSyntax block:
                return block.Statements.Any(IsEffectful);

            default:
                return false;
        }
    }

    public static bool IsEffectful(ExpressionSyntax expression)
    {
        switch (expression)
        {
            case MethodInvocationExpressionSyntax:
            case PostfixUnaryExpressionSyntax:
            case AssignmentExpressionSyntax:
                return true;

            case ValueExpressionSyntax value:
                return IsEffectful(value.Value);

            case ParenthesizedExpressionSyntax parenthesized:
                return IsEffectful(parenthesized.Expression);

            case UnaryExpressionSyntax unary:
                return IsEffectful(unary.Value);

            case BinaryExpressionSyntax binary:
                return IsEffectful(binary.Left) || IsEffectful(binary.Right);

            case LogicalExpressionSyntax logical:
                return IsEffectful(logical.Left) || IsEffectful(logical.Right);

            case ArrayIndexExpressionSyntax arrayIndex:
                return IsEffectful(arrayIndex.Value) ||
                       arrayIndex.Indexer.Any(indexer => IsEffectful(indexer.Index));

            case ArrayInstantiationExpressionSyntax arrayInstantiation:
                return arrayInstantiation.Indexer.Any(indexer => IsEffectful(indexer.Index));

            case TypeCastValueExpressionSyntax typeCast:
                return IsEffectful(typeCast.Value);

            case SwitchExpressionSyntax switchExpression:
                return IsEffectful(switchExpression.Value) ||
                       switchExpression.CaseBlock.Cases.Any(IsEffectfulCase);

            case VariableExpressionSyntax:
            case LiteralExpressionSyntax:
                return false;

            default:
                return true;
        }
    }

    public static bool IsPure(ExpressionSyntax expression) => !IsEffectful(expression);

    public static HashSet<string> CollectReadVariables(ExpressionSyntax expression)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        CollectReadVariables(expression, result);
        return result;
    }

    public static HashSet<string> CollectAssignedVariables(StatementSyntax statement)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        switch (statement)
        {
            case AssignmentStatementSyntax assignment:
                CollectAssignedTarget(assignment.Left, result);
                CollectAssignedTargets(assignment.Right, result);
                break;

            case PostfixUnaryStatementSyntax postfix:
                CollectAssignedTarget(postfix.Expression.Value, result);
                break;
        }

        return result;
    }

    private static bool IsEffectfulCase(SwitchCaseExpressionSyntax @case)
    {
        switch (@case)
        {
            case LiteralSwitchCaseExpressionSyntax literal:
                return IsEffectful(literal.CaseValue) || IsEffectful(literal.Value);

            case DefaultSwitchCaseExpressionSyntax defaultCase:
                return IsEffectful(defaultCase.Value);

            default:
                return true;
        }
    }

    private static void CollectReadVariables(ExpressionSyntax expression, HashSet<string> result)
    {
        switch (expression)
        {
            case VariableExpressionSyntax variable:
                result.Add(variable.Variable.Text);
                break;

            case ValueExpressionSyntax value:
                CollectReadVariables(value.Value, result);
                break;

            case ParenthesizedExpressionSyntax parenthesized:
                CollectReadVariables(parenthesized.Expression, result);
                break;

            case UnaryExpressionSyntax unary:
                CollectReadVariables(unary.Value, result);
                break;

            case BinaryExpressionSyntax binary:
                CollectReadVariables(binary.Left, result);
                CollectReadVariables(binary.Right, result);
                break;

            case LogicalExpressionSyntax logical:
                CollectReadVariables(logical.Left, result);
                CollectReadVariables(logical.Right, result);
                break;

            case MethodInvocationExpressionSyntax invocation:
                if (invocation.Parameters.ParameterList?.Elements is null)
                    break;
                foreach (ExpressionSyntax parameter in invocation.Parameters.ParameterList.Elements)
                    CollectReadVariables(parameter, result);
                break;

            case PostfixUnaryExpressionSyntax postfix:
                CollectReadVariables(postfix.Value, result);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                CollectReadVariables(arrayIndex.Value, result);
                foreach (ArrayIndexerExpressionSyntax indexer in arrayIndex.Indexer)
                    CollectReadVariables(indexer.Index, result);
                break;

            case ArrayInstantiationExpressionSyntax arrayInstantiation:
                foreach (ArrayIndexerExpressionSyntax indexer in arrayInstantiation.Indexer)
                    CollectReadVariables(indexer.Index, result);
                break;

            case TypeCastValueExpressionSyntax typeCast:
                CollectReadVariables(typeCast.Value, result);
                break;

            case SwitchExpressionSyntax switchExpression:
                CollectReadVariables(switchExpression.Value, result);
                foreach (SwitchCaseExpressionSyntax @case in switchExpression.CaseBlock.Cases)
                    CollectReadVariablesInCase(@case, result);
                break;

            case AssignmentExpressionSyntax assignment:
                CollectAssignmentExpressionReads(assignment, result);
                break;
        }
    }

    private static void CollectAssignmentExpressionReads(AssignmentExpressionSyntax assignment, HashSet<string> result)
    {
        // Bare destination variables are writes; array bases/indices are still reads.
        switch (assignment.Left)
        {
            case ValueExpressionSyntax { Value: VariableExpressionSyntax }:
            case VariableExpressionSyntax:
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                CollectReadVariables(arrayIndex.Value, result);
                foreach (ArrayIndexerExpressionSyntax indexer in arrayIndex.Indexer)
                    CollectReadVariables(indexer.Index, result);
                break;

            default:
                CollectReadVariables(assignment.Left, result);
                break;
        }

        CollectReadVariables(assignment.Right, result);
    }

    private static void CollectReadVariablesInCase(SwitchCaseExpressionSyntax @case, HashSet<string> result)
    {
        switch (@case)
        {
            case LiteralSwitchCaseExpressionSyntax literal:
                CollectReadVariables(literal.CaseValue, result);
                CollectReadVariables(literal.Value, result);
                break;

            case DefaultSwitchCaseExpressionSyntax defaultCase:
                CollectReadVariables(defaultCase.Value, result);
                break;
        }
    }

    private static void CollectAssignedTarget(ExpressionSyntax expression, HashSet<string> result)
    {
        switch (expression)
        {
            case VariableExpressionSyntax variable:
                result.Add(variable.Variable.Text);
                break;

            case ValueExpressionSyntax value:
                CollectAssignedTarget(value.Value, result);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                CollectAssignedTarget(arrayIndex.Value, result);
                break;

            case PostfixUnaryExpressionSyntax postfix:
                CollectAssignedTarget(postfix.Value, result);
                break;
        }
    }

    private static void CollectAssignedTargets(ExpressionSyntax expression, HashSet<string> result)
    {
        while (expression is AssignmentExpressionSyntax assignment)
        {
            CollectAssignedTarget(assignment.Left, result);
            expression = assignment.Right;
        }
    }
}
