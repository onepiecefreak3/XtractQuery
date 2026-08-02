using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal class TempExpressionRewriter(ILevel5SyntaxFactory syntaxFactory)
{
    public StatementSyntax? ReplaceTempInStatement(
        StatementSyntax statement,
        string tempName,
        ExpressionSyntax replacement,
        FoldContext context)
    {
        switch (statement)
        {
            case AssignmentStatementSyntax assignment:
                // Bare `$tempN = ...` is a def — never fold into the destination.
                // Array stores still read base/indices on the left and those may fold.
                ExpressionSyntax? left = ReplaceAssignmentLeftReads(assignment.Left, tempName, replacement);
                ExpressionSyntax? right = ReplaceTempInExpression(assignment.Right, tempName, replacement, context);
                if (left is null && right is null)
                    return null;
                return new AssignmentStatementSyntax(
                    left ?? assignment.Left,
                    assignment.EqualsOperator,
                    right ?? assignment.Right,
                    assignment.Semicolon);

            case IfGotoStatementSyntax ifGoto:
                ExpressionSyntax? value = ReplaceTempInExpression(ifGoto.Value, tempName, replacement, context);
                if (value is null)
                    return null;
                return new IfGotoStatementSyntax(ifGoto.If, value, ifGoto.Goto, ifGoto.Semicolon);

            case IfNotGotoStatementSyntax ifNotGoto:
                ExpressionSyntax? comparison = ReplaceTempInExpression(ifNotGoto.Comparison, tempName, replacement, context);
                if (comparison is null)
                    return null;
                return new IfNotGotoStatementSyntax(
                    ifNotGoto.If,
                    (UnaryExpressionSyntax)comparison,
                    ifNotGoto.Goto,
                    ifNotGoto.Semicolon);

            case ReturnStatementSyntax returnStatement:
                if (returnStatement.ValueExpression is null)
                    return null;
                ExpressionSyntax? returnValue = ReplaceTempInExpression(returnStatement.ValueExpression, tempName, replacement, context);
                if (returnValue is null)
                    return null;
                return new ReturnStatementSyntax(
                    returnStatement.Return,
                    returnValue,
                    returnStatement.Semicolon);

            case GotoStatementSyntax gotoStatement:
                return ReplaceTempInGotoTargets(gotoStatement, tempName, replacement, context);

            case PostfixUnaryStatementSyntax postfix:
                ExpressionSyntax? postfixExpr = ReplaceTempInExpression(postfix.Expression, tempName, replacement, context);
                if (postfixExpr is null)
                    return null;
                return new PostfixUnaryStatementSyntax((PostfixUnaryExpressionSyntax)postfixExpr, postfix.Semicolon);

            case MethodInvocationStatementSyntax invocation:
                MethodInvocationParametersSyntax? parameters =
                    ReplaceTempInInvocationParameters(invocation.Parameters, tempName, replacement, context);
                if (parameters is null)
                    return null;
                return new MethodInvocationStatementSyntax(invocation.Name, invocation.Metadata, parameters, invocation.Semicolon);

            default:
                return null;
        }
    }

    public ExpressionSyntax? ReplaceTempInExpression(
        ExpressionSyntax expression,
        string tempName,
        ExpressionSyntax replacement,
        FoldContext context)
    {
        switch (expression)
        {
            case VariableExpressionSyntax variable when IsTempVariable(variable, tempName):
                return ExpressionParenthesizer.MaybeParenthesize(
                    StripValueWrapper(replacement),
                    context.ParentPrecedence,
                    context.IsRightOperand,
                    syntaxFactory);

            case ValueExpressionSyntax value:
                ExpressionSyntax? inner = ReplaceTempInExpression(value.Value, tempName, replacement, context);
                if (inner is null)
                    return null;
                return new ValueExpressionSyntax(inner, value.MetadataParameters);

            case ParenthesizedExpressionSyntax parenthesized:
                ExpressionSyntax? parenInner = ReplaceTempInExpression(
                    parenthesized.Expression, tempName, replacement, FoldContext.None());
                if (parenInner is null)
                    return null;
                return new ParenthesizedExpressionSyntax(parenthesized.ParenOpen, parenInner, parenthesized.ParenClose);

            case UnaryExpressionSyntax unary:
                ExpressionSyntax? unaryValue = ReplaceTempInExpression(
                    unary.Value,
                    tempName,
                    replacement,
                    FoldContext.ForOperator(ExpressionPrecedence.Unary, isRightOperand: true));
                if (unaryValue is null)
                    return null;
                return new UnaryExpressionSyntax(unary.Operation, unaryValue);

            case BinaryExpressionSyntax binary:
                int binaryPrec = ExpressionPrecedence.GetOperatorPrecedence((SyntaxTokenKind)binary.Operation.RawKind)
                                 ?? ExpressionPrecedence.Primary;
                ExpressionSyntax? left = ReplaceTempInExpression(
                    binary.Left, tempName, replacement, FoldContext.ForOperator(binaryPrec, false));
                ExpressionSyntax? right = ReplaceTempInExpression(
                    binary.Right, tempName, replacement, FoldContext.ForOperator(binaryPrec, true));
                if (left is null && right is null)
                    return null;
                return new BinaryExpressionSyntax(left ?? binary.Left, binary.Operation, right ?? binary.Right);

            case AssignmentExpressionSyntax assignment:
                ExpressionSyntax? assignmentLeft = ReplaceAssignmentLeftReads(assignment.Left, tempName, replacement);
                ExpressionSyntax? assignmentRight = ReplaceTempInExpression(
                    assignment.Right, tempName, replacement, FoldContext.None());
                if (assignmentLeft is null && assignmentRight is null)
                    return null;
                return new AssignmentExpressionSyntax(
                    assignmentLeft ?? assignment.Left,
                    assignment.Operation,
                    assignmentRight ?? assignment.Right);

            case LogicalExpressionSyntax logical:
                int logicalPrec = ExpressionPrecedence.GetOperatorPrecedence((SyntaxTokenKind)logical.Operation.RawKind)
                                  ?? ExpressionPrecedence.Primary;
                ExpressionSyntax? logicalLeft = ReplaceTempInExpression(
                    logical.Left, tempName, replacement, FoldContext.ForOperator(logicalPrec, false));
                ExpressionSyntax? logicalRight = ReplaceTempInExpression(
                    logical.Right, tempName, replacement, FoldContext.ForOperator(logicalPrec, true));
                if (logicalLeft is null && logicalRight is null)
                    return null;
                return new LogicalExpressionSyntax(
                    logicalLeft ?? logical.Left, logical.Operation, logicalRight ?? logical.Right);

            case MethodInvocationExpressionSyntax invocation:
                MethodInvocationParametersSyntax? parameters =
                    ReplaceTempInInvocationParameters(invocation.Parameters, tempName, replacement, context);
                if (parameters is null)
                    return null;
                return new MethodInvocationExpressionSyntax(invocation.Name, invocation.Metadata, parameters);

            case PostfixUnaryExpressionSyntax postfix:
                ExpressionSyntax? postfixValue = ReplaceTempInExpression(
                    postfix.Value, tempName, replacement, FoldContext.ForOperator(ExpressionPrecedence.Postfix, false));
                if (postfixValue is null)
                    return null;
                return new PostfixUnaryExpressionSyntax(postfixValue, postfix.Operation);

            case ArrayIndexExpressionSyntax arrayIndex:
                ExpressionSyntax? arrayValue = ReplaceTempInExpression(arrayIndex.Value, tempName, replacement, FoldContext.None());
                if (arrayValue is null)
                    return null;
                return new ArrayIndexExpressionSyntax(
                    arrayValue as ValueExpressionSyntax ?? WrapAsValue(arrayValue),
                    arrayIndex.Indexer);

            case TypeCastValueExpressionSyntax typeCast:
                ExpressionSyntax? castValue = ReplaceTempInExpression(typeCast.Value, tempName, replacement, FoldContext.None());
                if (castValue is null)
                    return null;
                return new TypeCastValueExpressionSyntax(
                    typeCast.TypeCast,
                    castValue as ValueExpressionSyntax ?? WrapAsValue(castValue));

            case SwitchExpressionSyntax switchExpression:
                ExpressionSyntax? switchValue = ReplaceTempInExpression(
                    switchExpression.Value, tempName, replacement, FoldContext.None());
                if (switchValue is null)
                    return null;
                return new SwitchExpressionSyntax(switchValue, switchExpression.Switch, switchExpression.CaseBlock);

            default:
                return null;
        }
    }

    private ExpressionSyntax? ReplaceAssignmentLeftReads(
        ExpressionSyntax left,
        string tempName,
        ExpressionSyntax replacement)
    {
        switch (left)
        {
            case ValueExpressionSyntax { Value: VariableExpressionSyntax }:
                return null;

            case ValueExpressionSyntax value:
            {
                ExpressionSyntax? inner = ReplaceAssignmentLeftReads(value.Value, tempName, replacement);
                if (inner is null)
                    return null;
                return new ValueExpressionSyntax(inner, value.MetadataParameters);
            }

            case ArrayIndexExpressionSyntax arrayIndex:
            {
                ExpressionSyntax? arrayValue = ReplaceTempInExpression(
                    arrayIndex.Value, tempName, replacement, FoldContext.None());
                var indexers = new List<ArrayIndexerExpressionSyntax>();
                var replacedIndexer = false;
                foreach (ArrayIndexerExpressionSyntax indexer in arrayIndex.Indexer)
                {
                    ExpressionSyntax? rewrittenIndex = ReplaceTempInExpression(
                        indexer.Index, tempName, replacement, FoldContext.None());
                    if (rewrittenIndex is null)
                    {
                        indexers.Add(indexer);
                        continue;
                    }

                    replacedIndexer = true;
                    indexers.Add(new ArrayIndexerExpressionSyntax(
                        indexer.BracketOpen,
                        rewrittenIndex as ValueExpressionSyntax ?? WrapAsValue(rewrittenIndex),
                        indexer.BracketClose));
                }

                if (arrayValue is null && !replacedIndexer)
                    return null;

                return new ArrayIndexExpressionSyntax(
                    arrayValue as ValueExpressionSyntax ?? arrayIndex.Value,
                    replacedIndexer ? indexers : arrayIndex.Indexer);
            }

            default:
                return ReplaceTempInExpression(left, tempName, replacement, FoldContext.None());
        }
    }

    private StatementSyntax? ReplaceTempInGotoTargets(
        GotoStatementSyntax gotoStatement,
        string tempName,
        ExpressionSyntax replacement,
        FoldContext context)
    {
        if (gotoStatement.Targets?.Elements is null)
            return null;

        var targets = new List<ValueExpressionSyntax>();
        var replaced = false;
        foreach (ValueExpressionSyntax target in gotoStatement.Targets.Elements)
        {
            ExpressionSyntax? rewritten = ReplaceTempInExpression(target, tempName, replacement, context);
            if (rewritten is null)
            {
                targets.Add(target);
                continue;
            }

            replaced = true;
            targets.Add(rewritten as ValueExpressionSyntax ?? WrapAsValue(rewritten));
        }

        if (!replaced)
            return null;

        return new GotoStatementSyntax(
            gotoStatement.Goto,
            new CommaSeparatedSyntaxList<ValueExpressionSyntax>(targets),
            gotoStatement.Semicolon);
    }

    private MethodInvocationParametersSyntax? ReplaceTempInInvocationParameters(
        MethodInvocationParametersSyntax parameters,
        string tempName,
        ExpressionSyntax replacement,
        FoldContext context)
    {
        if (parameters.ParameterList?.Elements is null)
            return null;

        var elements = new List<ExpressionSyntax>();
        var replaced = false;
        foreach (ExpressionSyntax parameter in parameters.ParameterList.Elements)
        {
            ExpressionSyntax? rewritten = ReplaceTempInExpression(parameter, tempName, replacement, FoldContext.None());
            if (rewritten is null)
            {
                elements.Add(parameter);
                continue;
            }

            replaced = true;
            elements.Add(rewritten);
        }

        if (!replaced)
            return null;

        return new MethodInvocationParametersSyntax(
            parameters.ParenOpen,
            new CommaSeparatedSyntaxList<ExpressionSyntax>(elements),
            parameters.ParenClose);
    }

    private static ExpressionSyntax StripValueWrapper(ExpressionSyntax expression)
    {
        if (expression is ValueExpressionSyntax { MetadataParameters: null } value)
            return value.Value;

        return expression;
    }

    private static ValueExpressionSyntax WrapAsValue(ExpressionSyntax expression)
    {
        if (expression is ValueExpressionSyntax value)
            return value;

        return new ValueExpressionSyntax(expression);
    }

    private static bool IsTempVariable(VariableExpressionSyntax variable, string tempName)
    {
        return variable.Variable.Text == tempName;
    }

    internal readonly record struct FoldContext(int ParentPrecedence, bool IsRightOperand)
    {
        public static FoldContext None() => new(ExpressionPrecedence.None, false);
        public static FoldContext ForOperator(int precedence, bool isRightOperand) => new(precedence, isRightOperand);
    }
}
