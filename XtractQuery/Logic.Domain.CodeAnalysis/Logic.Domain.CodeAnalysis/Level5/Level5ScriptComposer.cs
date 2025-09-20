using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using System.Text;

namespace Logic.Domain.CodeAnalysis.Level5;

internal class Level5ScriptComposer : ILevel5ScriptComposer
{
    private readonly ILevel5SyntaxFactory _syntaxFactory;

    public Level5ScriptComposer(ILevel5SyntaxFactory syntaxFactory)
    {
        _syntaxFactory = syntaxFactory;
    }

    public string ComposeCodeUnit(CodeUnitSyntax codeUnit)
    {
        var sb = new StringBuilder();

        ComposeCodeUnit(codeUnit, sb);

        return sb.ToString();
    }

    public string ComposeMethodDeclaration(MethodDeclarationSyntax methodDeclaration)
    {
        var sb = new StringBuilder();

        ComposeMethodDeclaration(methodDeclaration, sb);

        return sb.ToString();
    }

    public string ComposeMethodDeclarationMetadataParameters(
        MethodDeclarationMetadataParametersSyntax methodDeclarationMetadataParameters)
    {
        var sb = new StringBuilder();

        ComposeMethodDeclarationMetadataParameters(methodDeclarationMetadataParameters, sb);

        return sb.ToString();
    }

    public string ComposeMethodDeclarationMetadataParameterList(
        MethodDeclarationMetadataParameterListSyntax methodDeclarationMetadataParameterList)
    {
        var sb = new StringBuilder();

        ComposeMethodDeclarationMetadataParameterList(methodDeclarationMetadataParameterList, sb);

        return sb.ToString();
    }

    public string ComposeMethodDeclarationParameters(MethodDeclarationParametersSyntax methodDeclarationParameters)
    {
        var sb = new StringBuilder();

        ComposeMethodDeclarationParameters(methodDeclarationParameters, sb);

        return sb.ToString();
    }

    public string ComposeMethodDeclarationBody(MethodDeclarationBodySyntax methodDeclarationBody)
    {
        var sb = new StringBuilder();

        ComposeMethodDeclarationBody(methodDeclarationBody, sb);

        return sb.ToString();
    }

    public string ComposeStatement(StatementSyntax statement)
    {
        var sb = new StringBuilder();

        ComposeStatement(statement, sb);

        return sb.ToString();
    }

    public string ComposeGotoLabelStatement(GotoLabelStatementSyntax gotoLabelStatement)
    {
        var sb = new StringBuilder();

        ComposeGotoLabelStatement(gotoLabelStatement, sb);

        return sb.ToString();
    }

    public string ComposeReturnStatement(ReturnStatementSyntax returnStatement)
    {
        var sb = new StringBuilder();

        ComposeReturnStatement(returnStatement, sb);

        return sb.ToString();
    }

    public string ComposeMethodInvocationExpression(MethodInvocationExpressionSyntax invocation)
    {
        var sb = new StringBuilder();

        ComposeMethodInvocationExpression(invocation, sb);

        return sb.ToString();
    }

    public string ComposeMethodInvocationExpressionParameters(MethodInvocationExpressionParametersSyntax invocationParameters)
    {
        var sb = new StringBuilder();

        ComposeMethodInvocationExpressionParameters(invocationParameters, sb);

        return sb.ToString();
    }

    public string ComposeValueList(CommaSeparatedSyntaxList<ValueExpressionSyntax> valueList)
    {
        var sb = new StringBuilder();

        ComposeValueExpressions(valueList, sb);

        return sb.ToString();
    }

    public string ComposeValue(ValueExpressionSyntax valueExpression)
    {
        var sb = new StringBuilder();

        ComposeValueExpression(valueExpression, sb);

        return sb.ToString();
    }

    public string ComposeValueMetadataParameters(ValueMetadataParametersSyntax valueMetadataParameters)
    {
        var sb = new StringBuilder();

        ComposeValueMetadataParameters(valueMetadataParameters, sb);

        return sb.ToString();
    }


    private void ComposeCodeUnit(CodeUnitSyntax codeUnit, StringBuilder sb)
    {
        foreach (MethodDeclarationSyntax methodDeclaration in codeUnit.MethodDeclarations)
            ComposeMethodDeclaration(methodDeclaration, sb);
    }

    private void ComposeMethodDeclaration(MethodDeclarationSyntax methodDeclaration, StringBuilder sb)
    {
        ComposeSyntaxToken(methodDeclaration.Identifier, sb);
        ComposeMethodDeclarationMetadataParameters(methodDeclaration.MetadataParameters, sb);
        ComposeMethodDeclarationParameters(methodDeclaration.Parameters, sb);
        ComposeMethodDeclarationBody(methodDeclaration.Body, sb);
    }

    private void ComposeMethodDeclarationMetadataParameters(
        MethodDeclarationMetadataParametersSyntax? methodDeclarationMetadataParameters, StringBuilder sb)
    {
        if (methodDeclarationMetadataParameters == null)
            return;

        ComposeSyntaxToken(methodDeclarationMetadataParameters.RelSmaller, sb);
        ComposeMethodDeclarationMetadataParameterList(methodDeclarationMetadataParameters.List, sb);
        ComposeSyntaxToken(methodDeclarationMetadataParameters.RelBigger, sb);
    }

    private void ComposeMethodDeclarationMetadataParameterList(
        MethodDeclarationMetadataParameterListSyntax methodDeclarationMetadataParameterList, StringBuilder sb)
    {
        ComposeLiteralExpression(methodDeclarationMetadataParameterList.Parameter1, sb);
        ComposeSyntaxToken(methodDeclarationMetadataParameterList.Comma, sb);
        ComposeLiteralExpression(methodDeclarationMetadataParameterList.Parameter2, sb);
    }

    private void ComposeMethodDeclarationParameters(MethodDeclarationParametersSyntax methodDeclarationParameters, StringBuilder sb)
    {
        ComposeSyntaxToken(methodDeclarationParameters.ParenOpen, sb);
        ComposeVariableExpressions(methodDeclarationParameters.Parameters, sb);
        ComposeSyntaxToken(methodDeclarationParameters.ParenClose, sb);
    }

    private void ComposeVariableExpressions(CommaSeparatedSyntaxList<VariableExpressionSyntax>? valueList, StringBuilder sb)
    {
        if (valueList == null || valueList.Elements.Count <= 0)
            return;

        for (var i = 0; i < valueList.Elements.Count - 1; i++)
        {
            ComposeVariableExpression(valueList.Elements[i], sb);
            ComposeSyntaxToken(_syntaxFactory.Token(SyntaxTokenKind.Comma), sb);
        }

        ComposeVariableExpression(valueList.Elements[^1], sb);
    }

    private void ComposeMethodDeclarationBody(MethodDeclarationBodySyntax methodDeclarationBody, StringBuilder sb)
    {
        ComposeSyntaxToken(methodDeclarationBody.CurlyOpen, sb);

        foreach (StatementSyntax expression in methodDeclarationBody.Expressions)
            ComposeStatement(expression, sb);

        ComposeSyntaxToken(methodDeclarationBody.CurlyClose, sb);
    }

    private void ComposeStatement(StatementSyntax statement, StringBuilder sb)
    {
        switch (statement)
        {
            case GotoLabelStatementSyntax gotoStatement:
                ComposeGotoLabelStatement(gotoStatement, sb);
                break;

            case YieldStatementSyntax yieldStatement:
                ComposeYieldStatement(yieldStatement, sb);
                break;

            case ReturnStatementSyntax returnStatement:
                ComposeReturnStatement(returnStatement, sb);
                break;

            case ExitStatementSyntax exitStatement:
                ComposeExitStatement(exitStatement, sb);
                break;

            case IfGotoStatementSyntax ifGotoStatement:
                ComposeIfGotoStatement(ifGotoStatement, sb);
                break;

            case GotoStatementSyntax gotoStatement:
                ComposeGotoStatement(gotoStatement, sb);
                break;

            case IfNotGotoStatementSyntax ifNotGotoStatement:
                ComposeIfNotGotoStatement(ifNotGotoStatement, sb);
                break;

            case PostfixUnaryStatementSyntax postfixUnaryStatement:
                ComposePostfixUnaryStatement(postfixUnaryStatement, sb);
                break;

            case AssignmentStatementSyntax assignmentStatement:
                ComposeAssignmentExpression(assignmentStatement, sb);
                break;
        }
    }

    private void ComposePostfixUnaryStatement(PostfixUnaryStatementSyntax postfixUnaryStatement, StringBuilder sb)
    {
        ComposeExpression(postfixUnaryStatement.Expression, sb);
        ComposeSyntaxToken(postfixUnaryStatement.Semicolon, sb);
    }

    private void ComposeIfGotoStatement(IfGotoStatementSyntax ifGotoStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(ifGotoStatement.If, sb);
        ComposeValueExpression(ifGotoStatement.Value, sb);
        ComposeGotoStatement(ifGotoStatement.Goto, sb);
    }

    private void ComposeGotoStatement(GotoStatementSyntax gotoStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(gotoStatement.Goto, sb);
        ComposeValueExpression(gotoStatement.Target, sb);
        ComposeSyntaxToken(gotoStatement.Semicolon, sb);
    }

    private void ComposeIfNotGotoStatement(IfNotGotoStatementSyntax ifNotGotoStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(ifNotGotoStatement.If, sb);
        ComposeUnaryExpression(ifNotGotoStatement.Comparison, sb);
        ComposeGotoStatement(ifNotGotoStatement.Goto, sb);
    }

    private void ComposeGotoLabelStatement(GotoLabelStatementSyntax gotoLabelStatement, StringBuilder sb)
    {
        ComposeLiteralExpression(gotoLabelStatement.Label, sb);
        ComposeSyntaxToken(gotoLabelStatement.Colon, sb);
    }

    private void ComposeYieldStatement(YieldStatementSyntax returnStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(returnStatement.Yield, sb);
        ComposeSyntaxToken(returnStatement.Semicolon, sb);
    }

    private void ComposeReturnStatement(ReturnStatementSyntax returnStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(returnStatement.Return, sb);
        if (returnStatement.ValueExpression != null)
            ComposeValueExpression(returnStatement.ValueExpression, sb);
        ComposeSyntaxToken(returnStatement.Semicolon, sb);
    }

    private void ComposeExitStatement(ExitStatementSyntax exitStatement, StringBuilder sb)
    {
        ComposeSyntaxToken(exitStatement.Exit, sb);
        ComposeSyntaxToken(exitStatement.Semicolon, sb);
    }

    private void ComposeAssignmentExpression(AssignmentStatementSyntax assignmentStatement, StringBuilder sb)
    {
        ComposeExpression(assignmentStatement.Left, sb);
        ComposeSyntaxToken(assignmentStatement.EqualsOperator, sb);
        ComposeExpression(assignmentStatement.Right, sb);
        ComposeSyntaxToken(assignmentStatement.Semicolon, sb);
    }

    private void ComposeExpression(ExpressionSyntax expression, StringBuilder sb)
    {
        switch (expression)
        {
            case TypeCastValueExpressionSyntax typeCastValueExpression:
                ComposeTypeCastValueExpression(typeCastValueExpression, sb);
                break;

            case PostfixUnaryExpressionSyntax postfixUnaryExpression:
                ComposePostfixUnaryExpression(postfixUnaryExpression, sb);
                break;

            case SwitchExpressionSyntax switchExpression:
                ComposeSwitchExpression(switchExpression, sb);
                break;

            case LogicalExpressionSyntax logicalExpression:
                ComposeLogicalExpression(logicalExpression, sb);
                break;

            case UnaryExpressionSyntax unaryExpression:
                ComposeUnaryExpression(unaryExpression, sb);
                break;

            case BinaryExpressionSyntax binaryExpression:
                ComposeBinaryExpression(binaryExpression, sb);
                break;

            case ArrayInstantiationExpressionSyntax arrayInstantiation:
                ComposeArrayInstantiationExpression(arrayInstantiation, sb);
                break;

            case ArrayIndexExpressionSyntax arrayIndex:
                ComposeArrayIndexExpression(arrayIndex, sb);
                break;

            case MethodInvocationExpressionSyntax methodInvocation:
                ComposeMethodInvocationExpression(methodInvocation, sb);
                break;

            case ValueExpressionSyntax valueExpression:
                ComposeValueExpression(valueExpression, sb);
                break;

            case VariableExpressionSyntax variableExpression:
                ComposeVariableExpression(variableExpression, sb);
                break;

            case LiteralExpressionSyntax literalExpression:
                ComposeLiteralExpression(literalExpression, sb);
                break;
        }
    }

    private void ComposeTypeCastValueExpression(TypeCastValueExpressionSyntax typeCastValueExpression, StringBuilder sb)
    {
        ComposeTypeCastExpression(typeCastValueExpression.TypeCast, sb);
        ComposeValueExpression(typeCastValueExpression.Value, sb);
    }

    private void ComposeTypeCastExpression(TypeCastExpressionSyntax typeCastExpression, StringBuilder sb)
    {
        ComposeSyntaxToken(typeCastExpression.ParenOpen, sb);
        ComposeSyntaxToken(typeCastExpression.TypeKeyword, sb);
        ComposeSyntaxToken(typeCastExpression.ParenClose, sb);
    }

    private void ComposePostfixUnaryExpression(PostfixUnaryExpressionSyntax postfixUnaryExpression, StringBuilder sb)
    {
        ComposeExpression(postfixUnaryExpression.Value, sb);
        ComposeSyntaxToken(postfixUnaryExpression.Operation, sb);
    }

    private void ComposeSwitchExpression(SwitchExpressionSyntax switchExpression, StringBuilder sb)
    {
        ComposeExpression(switchExpression.Value, sb);
        ComposeSyntaxToken(switchExpression.Switch, sb);
        ComposeSwitchBlockExpression(switchExpression.CaseBlock, sb);
    }

    private void ComposeSwitchBlockExpression(SwitchBlockExpressionSyntax caseBlock, StringBuilder sb)
    {
        ComposeSyntaxToken(caseBlock.CurlyOpen, sb);
        foreach (var @case in caseBlock.Cases)
            ComposeSwitchCaseExpression(@case, sb);
        ComposeSyntaxToken(caseBlock.CurlyClose, sb);
    }

    private void ComposeSwitchCaseExpression(SwitchCaseExpressionSyntax @case, StringBuilder sb)
    {
        switch (@case)
        {
            case DefaultSwitchCaseExpressionSyntax defaultCase:
                ComposeDefaultSwitchCaseExpression(defaultCase, sb);
                break;

            case LiteralSwitchCaseExpressionSyntax literalCase:
                ComposeLiteralSwitchCaseExpression(literalCase, sb);
                break;
        }
    }

    private void ComposeDefaultSwitchCaseExpression(DefaultSwitchCaseExpressionSyntax defaultCase, StringBuilder sb)
    {
        ComposeSyntaxToken(defaultCase.Underscore, sb);
        ComposeSyntaxToken(defaultCase.ArrowRight, sb);
        ComposeValueExpression(defaultCase.Value, sb);
    }

    private void ComposeLiteralSwitchCaseExpression(LiteralSwitchCaseExpressionSyntax literalCase, StringBuilder sb)
    {
        ComposeValueExpression(literalCase.CaseValue, sb);
        ComposeSyntaxToken(literalCase.ArrowRight, sb);
        ComposeValueExpression(literalCase.Value, sb);
    }

    private void ComposeLogicalExpression(LogicalExpressionSyntax logicalExpression, StringBuilder sb)
    {
        ComposeExpression(logicalExpression.Left, sb);
        ComposeSyntaxToken(logicalExpression.Operation, sb);
        ComposeExpression(logicalExpression.Right, sb);
    }

    private void ComposeUnaryExpression(UnaryExpressionSyntax unaryExpression, StringBuilder sb)
    {
        ComposeSyntaxToken(unaryExpression.Operation, sb);
        ComposeValueExpression(unaryExpression.Value, sb);
    }

    private void ComposeBinaryExpression(BinaryExpressionSyntax binaryExpression, StringBuilder sb)
    {
        ComposeExpression(binaryExpression.Left, sb);
        ComposeSyntaxToken(binaryExpression.Operation, sb);
        ComposeExpression(binaryExpression.Right, sb);
    }

    private void ComposeArrayInstantiationExpression(ArrayInstantiationExpressionSyntax arrayInstantiation,
        StringBuilder sb)
    {
        ComposeSyntaxToken(arrayInstantiation.New, sb);
        foreach (var index in arrayInstantiation.Indexer)
            ComposeArrayIndexerExpression(index, sb);
    }

    private void ComposeArrayIndexExpression(ArrayIndexExpressionSyntax arrayIndex, StringBuilder sb)
    {
        ComposeExpression(arrayIndex.Value, sb);
        foreach (var index in arrayIndex.Indexer)
            ComposeArrayIndexerExpression(index, sb);
    }

    private void ComposeArrayIndexerExpression(ArrayIndexerExpressionSyntax indexer, StringBuilder sb)
    {
        ComposeSyntaxToken(indexer.BracketOpen, sb);
        ComposeValueExpression(indexer.Index, sb);
        ComposeSyntaxToken(indexer.BracketClose, sb);
    }

    private void ComposeMethodInvocationExpression(MethodInvocationExpressionSyntax invocation, StringBuilder sb)
    {
        ComposeSyntaxToken(invocation.Identifier, sb);
        ComposeMethodInvocationMetadata(invocation.Metadata, sb);
        ComposeMethodInvocationExpressionParameters(invocation.Parameters, sb);
    }

    private void ComposeMethodInvocationMetadata(MethodInvocationMetadataSyntax? metadata, StringBuilder sb)
    {
        if (metadata == null)
            return;

        ComposeSyntaxToken(metadata.RelSmaller, sb);
        ComposeLiteralExpression(metadata.Parameter, sb);
        ComposeSyntaxToken(metadata.RelBigger, sb);
    }

    private void ComposeMethodInvocationExpressionParameters(MethodInvocationExpressionParametersSyntax invocationParameters, StringBuilder sb)
    {
        ComposeSyntaxToken(invocationParameters.ParenOpen, sb);
        ComposeValueExpressions(invocationParameters.ParameterList, sb);
        ComposeSyntaxToken(invocationParameters.ParenClose, sb);
    }

    private void ComposeValueExpressions(CommaSeparatedSyntaxList<ValueExpressionSyntax>? valueList, StringBuilder sb)
    {
        if (valueList == null || valueList.Elements.Count <= 0)
            return;

        for (var i = 0; i < valueList.Elements.Count - 1; i++)
        {
            ComposeValueExpression(valueList.Elements[i], sb);
            ComposeSyntaxToken(_syntaxFactory.Token(SyntaxTokenKind.Comma), sb);
        }

        ComposeValueExpression(valueList.Elements[^1], sb);
    }

    public void ComposeValueExpression(ValueExpressionSyntax valueExpression, StringBuilder sb)
    {
        ComposeExpression(valueExpression.Value, sb);
        ComposeValueMetadataParameters(valueExpression.MetadataParameters, sb);
    }

    private void ComposeVariableExpression(VariableExpressionSyntax variable, StringBuilder sb)
    {
        ComposeSyntaxToken(variable.Variable, sb);
    }

    private void ComposeLiteralExpression(LiteralExpressionSyntax literal, StringBuilder sb)
    {
        ComposeSyntaxToken(literal.Literal, sb);
    }

    public void ComposeValueMetadataParameters(ValueMetadataParametersSyntax? valueMetadataParameters, StringBuilder sb)
    {
        if (valueMetadataParameters == null)
            return;

        ComposeSyntaxToken(valueMetadataParameters.RelSmaller, sb);
        ComposeLiteralExpression(valueMetadataParameters.Parameter, sb);
        ComposeSyntaxToken(valueMetadataParameters.RelBigger, sb);
    }

    private void ComposeSyntaxToken(SyntaxToken token, StringBuilder sb)
    {
        if (token.LeadingTrivia.HasValue)
            sb.Append(token.LeadingTrivia.Value.Text);

        sb.Append(token.Text);

        if (token.TrailingTrivia.HasValue)
            sb.Append(token.TrailingTrivia.Value.Text);
    }
}