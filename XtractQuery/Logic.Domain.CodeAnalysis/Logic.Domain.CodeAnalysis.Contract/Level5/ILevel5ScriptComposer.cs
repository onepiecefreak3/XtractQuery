using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Domain.CodeAnalysis.Contract.Level5;

public interface ILevel5ScriptComposer
{
    string ComposeCodeUnit(CodeUnitSyntax codeUnit);
    string ComposeMethodDeclaration(MethodDeclarationSyntax methodDeclaration);
    string ComposeMethodDeclarationMetadataParameters(MethodDeclarationMetadataParametersSyntax methodDeclarationMetadataParameters);
    string ComposeMethodDeclarationMetadataParameterList(MethodDeclarationMetadataParameterListSyntax methodDeclarationMetadataParameterList);
    string ComposeMethodDeclarationParameters(MethodDeclarationParametersSyntax methodDeclarationParameters);
    string ComposeMethodDeclarationBody(MethodDeclarationBodySyntax methodDeclarationBody);
    string ComposeStatement(StatementSyntax statement);
    string ComposeReturnStatement(ReturnStatementSyntax returnStatement);
    string ComposeGotoLabelStatement(GotoLabelStatementSyntax gotoLabelStatement);
    string ComposeMethodInvocationExpression(MethodInvocationExpressionSyntax invocation);
    string ComposeMethodInvocationParameters(MethodInvocationParametersSyntax invocationParameters);
    string ComposeValueList(CommaSeparatedSyntaxList<ValueExpressionSyntax> valueList);
    string ComposeValue(ValueExpressionSyntax valueExpression);
    string ComposeValueMetadataParameters(ValueMetadataParametersSyntax valueMetadataParameters);
}