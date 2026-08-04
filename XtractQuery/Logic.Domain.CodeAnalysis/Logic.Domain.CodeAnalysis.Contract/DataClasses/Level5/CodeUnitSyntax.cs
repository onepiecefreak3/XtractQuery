namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class CodeUnitSyntax : SyntaxNode
{
    public IReadOnlyList<CodeUnitMemberSyntax> Members { get; private set; }

    public IReadOnlyList<MethodDeclarationSyntax> MethodDeclarations =>
        Members.OfType<MethodDeclarationSyntax>().ToList();

    public IReadOnlyList<GlobalDeclarationStatementSyntax> GlobalDeclarations =>
        Members.OfType<GlobalDeclarationStatementSyntax>().ToList();

    public override SyntaxLocation Location => Members.Count > 0 ? Members[0].Location : new(1, 1);
    public override SyntaxSpan Span => new(Members.Count > 0 ? Members[0].Span.Position : 0,
        Members.Count > 0 ? Members[^1].Span.EndPosition : 0);

    public CodeUnitSyntax(IReadOnlyList<CodeUnitMemberSyntax>? members)
    {
        Members = members ?? new List<CodeUnitMemberSyntax>();

        foreach (CodeUnitMemberSyntax member in Members)
            member.Parent = this;

        Root.Update();
    }

    public CodeUnitSyntax(IReadOnlyList<MethodDeclarationSyntax>? methodDeclarations)
        : this(methodDeclarations?.Cast<CodeUnitMemberSyntax>().ToList())
    {
    }

    public void SetMembers(IReadOnlyList<CodeUnitMemberSyntax>? members, bool updatePosition = true)
    {
        Members = members ?? new List<CodeUnitMemberSyntax>();
        foreach (CodeUnitMemberSyntax member in Members)
            member.Parent = this;

        if (updatePosition)
            Root.Update();
    }

    public void SetMethodDeclarations(IReadOnlyList<MethodDeclarationSyntax>? methodDeclarations, bool updatePosition = true)
    {
        SetMembers(methodDeclarations?.Cast<CodeUnitMemberSyntax>().ToList(), updatePosition);
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        foreach (CodeUnitMemberSyntax member in Members)
            position = member.UpdatePosition(position, ref line, ref column);

        return position;
    }
}
