using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal class HighLevelCodeUnitConverter(
    ITempPropagationPass tempPropagationPass,
    IChainAssignmentFoldPass chainAssignmentFoldPass,
    IStructuredLoopPass structuredLoopPass,
    IStructuredIfPass structuredIfPass) : IHighLevelCodeUnitConverter
{
    public CodeUnitSyntax Convert(CodeUnitSyntax tree)
    {
        var methods = new List<MethodDeclarationSyntax>();
        foreach (MethodDeclarationSyntax method in tree.MethodDeclarations)
            methods.Add(ConvertMethod(method));

        return new CodeUnitSyntax(methods);
    }

    private MethodDeclarationSyntax ConvertMethod(MethodDeclarationSyntax method)
    {
        IReadOnlyList<StatementSyntax> statements = tempPropagationPass.Apply(method.Body.Expressions);
        statements = chainAssignmentFoldPass.Apply(statements);
        statements = structuredLoopPass.Apply(statements);
        statements = structuredIfPass.Apply(statements);

        var body = new MethodDeclarationBodySyntax(method.Body.CurlyOpen, statements, method.Body.CurlyClose);
        return new MethodDeclarationSyntax(method.Identifier, method.MetadataParameters, method.Parameters, body);
    }
}
