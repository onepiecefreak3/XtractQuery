using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5.Exceptions;

namespace Logic.Domain.CodeAnalysis.Contract.Level5;

[MapException(typeof(Level5ScriptWhitespaceNormalizer))]
public interface ILevel5ScriptWhitespaceNormalizer
{
    void NormalizeCodeUnit(CodeUnitSyntax codeUnit);
    void NormalizeMethodDeclaration(MethodDeclarationSyntax methodDeclaration);
    void NormalizeMethodDeclarationMetadataParameters(MethodDeclarationMetadataParametersSyntax methodDeclarationMetadataParameters);
    void NormalizeMethodDeclarationMetadataParameterList(MethodDeclarationMetadataParameterListSyntax methodDeclarationMetadataParameterList);
    void NormalizeMethodDeclarationParameters(MethodDeclarationParametersSyntax methodDeclarationParameters);
    void NormalizeMethodDeclarationBody(MethodDeclarationBodySyntax methodDeclarationBody);
    void NormalizeGotoLabelStatement(GotoLabelStatementSyntax gotoLabelStatement);
    void NormalizeReturnStatement(ReturnStatementSyntax returnStatement);
    void NormalizeMethodInvocationExpression(MethodInvocationExpressionSyntax invocation);
    void NormalizeMethodInvocationParameters(MethodInvocationParametersSyntax invocationParameters);
    void NormalizeValueList(CommaSeparatedSyntaxList<ValueExpressionSyntax> valueList);
    void NormalizeValue(ValueExpressionSyntax valueExpression);
    void NormalizeValueMetadataParameters(ValueMetadataParametersSyntax valueMetadataParameters);
}