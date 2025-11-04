using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Domain.CodeAnalysis.Contract.Level5;

public interface ILevel5ScriptParser
{
    CodeUnitSyntax ParseCodeUnit(string text);
    MethodDeclarationSyntax ParseMethodDeclaration(string text);
    MethodDeclarationMetadataParametersSyntax? ParseMethodDeclarationMetadataParameters(string text);
    MethodDeclarationMetadataParameterListSyntax ParseMethodDeclarationMetadataParameterList(string text);
    MethodDeclarationParametersSyntax ParseMethodDeclarationParameters(string text);
    MethodDeclarationBodySyntax ParseMethodDeclarationBody(string text);
    StatementSyntax ParseStatement(string text);
    GotoLabelStatementSyntax ParseGotoLabelStatement(string text);
    ReturnStatementSyntax ParseReturnStatement(string text);
    MethodInvocationExpressionSyntax ParseMethodInvocationExpression(string text);
    MethodInvocationStatementSyntax ParseMethodInvocationStatement(string text);
    MethodInvocationParametersSyntax ParseMethodInvocationParameters(string text);
    CommaSeparatedSyntaxList<ValueExpressionSyntax>? ParseValueList(string text);
    ValueExpressionSyntax ParseValueExpression(string text);
    ValueMetadataParametersSyntax? ParseValueMetadataParameters(string text);
}