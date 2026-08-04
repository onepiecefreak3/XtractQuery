using Logic.Business.Level5ScriptManagement.InternalContract.Conversion.HighLevelSyntax;
using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Business.Level5ScriptManagement.Conversion.HighLevelSyntax;

internal class HighLevelCodeUnitConverter(
    ITempPropagationPass tempPropagationPass,
    IChainAssignmentFoldPass chainAssignmentFoldPass,
    IStructuredLoopPass structuredLoopPass,
    IStructuredIfPass structuredIfPass,
    IStructuredForPass structuredForPass) : IHighLevelCodeUnitConverter
{
    public CodeUnitSyntax Convert(CodeUnitSyntax tree)
    {
        var members = new List<CodeUnitMemberSyntax>();
        foreach (CodeUnitMemberSyntax member in tree.Members)
        {
            if (member is MethodDeclarationSyntax method)
                members.Add(ConvertMethod(method));
            else
                members.Add(member);
        }

        return new CodeUnitSyntax(members);
    }

    private MethodDeclarationSyntax ConvertMethod(MethodDeclarationSyntax method)
    {
        IReadOnlyList<StatementSyntax> statements = tempPropagationPass.Apply(method.Body.Expressions);
        statements = chainAssignmentFoldPass.Apply(statements);

        // Loop and if raising interdepend when jump-table hash sort co-locates labels
        // (e.g. empty-else join with a spin head). Alternate to a fixpoint.
        for (var i = 0; i < 8; i++)
        {
            IReadOnlyList<StatementSyntax> afterLoops = structuredLoopPass.Apply(statements);
            IReadOnlyList<StatementSyntax> afterIfs = structuredIfPass.Apply(afterLoops);
            if (ReferenceEquals(afterIfs, statements) || StatementListsEqual(afterIfs, statements))
            {
                statements = afterIfs;
                break;
            }

            statements = afterIfs;
        }

        // For-raise needs structured while + break/continue already in place.
        statements = structuredForPass.Apply(statements);

        var body = new MethodDeclarationBodySyntax(method.Body.CurlyOpen, statements, method.Body.CurlyClose);
        return new MethodDeclarationSyntax(method.Identifier, method.MetadataParameters, method.Parameters, body);
    }

    private static bool StatementListsEqual(
        IReadOnlyList<StatementSyntax> left,
        IReadOnlyList<StatementSyntax> right)
    {
        if (left.Count != right.Count)
            return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!ReferenceEquals(left[i], right[i]))
                return false;
        }

        return true;
    }
}
