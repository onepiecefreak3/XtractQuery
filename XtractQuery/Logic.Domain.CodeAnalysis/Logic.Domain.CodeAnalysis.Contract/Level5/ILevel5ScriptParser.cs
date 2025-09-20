using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5.Exceptions;

namespace Logic.Domain.CodeAnalysis.Contract.Level5;

[MapException(typeof(Level5ScriptParserException))]
public interface ILevel5ScriptParser
{
    CodeUnitSyntax ParseCodeUnit(string text);
    MethodDeclarationSyntax ParseMethodDeclaration(string text);
    MethodDeclarationMetadataParametersSyntax ParseMethodDeclarationMetadataParameters(string text);
    MethodDeclarationMetadataParameterListSyntax ParseMethodDeclarationMetadataParameterList(string text);
    MethodDeclarationParametersSyntax ParseMethodDeclarationParameters(string text);
    MethodDeclarationBodySyntax ParseMethodDeclarationBody(string text);
    StatementSyntax ParseStatement(string text);
    GotoLabelStatementSyntax ParseGotoLabelStatement(string text);
    ReturnStatementSyntax ParseReturnStatement(string text);
    MethodInvocationExpressionSyntax ParseMethodInvocationExpression(string text);
    MethodInvocationExpressionParametersSyntax ParseMethodInvocationExpressionParameters(string text);
    CommaSeparatedSyntaxList<ValueExpressionSyntax>? ParseValueList(string text);
    ValueExpressionSyntax ParseValueExpression(string text);
    ValueMetadataParametersSyntax? ParseValueMetadataParameters(string text);
}