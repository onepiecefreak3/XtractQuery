using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class CodeUnitSyntax : SyntaxNode
{
    public IReadOnlyList<MethodDeclarationSyntax> MethodDeclarations { get; private set; }

    public override SyntaxLocation Location => MethodDeclarations.Count > 0 ? MethodDeclarations[0].Location : new(1, 1);
    public override SyntaxSpan Span => new(MethodDeclarations.Count > 0 ? MethodDeclarations[0].Span.Position : 0,
        MethodDeclarations.Count > 0 ? MethodDeclarations[^1].Span.EndPosition : 0);

    public CodeUnitSyntax(IReadOnlyList<MethodDeclarationSyntax>? methodDeclarations)
    {
        MethodDeclarations = methodDeclarations ?? new List<MethodDeclarationSyntax>();

        foreach (MethodDeclarationSyntax methodDeclaration in MethodDeclarations)
            methodDeclaration.Parent = this;

        Root.Update();
    }

    public void SetMethodDeclarations(IReadOnlyList<MethodDeclarationSyntax>? methodDeclarations, bool updatePosition = true)
    {
        MethodDeclarations = methodDeclarations ?? new List<MethodDeclarationSyntax>();
        foreach (MethodDeclarationSyntax methodDeclaration in MethodDeclarations)
            methodDeclaration.Parent = this;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        foreach (MethodDeclarationSyntax methodDeclaration in MethodDeclarations)
            position = methodDeclaration.UpdatePosition(position, ref line, ref column);

        return position;
    }
}