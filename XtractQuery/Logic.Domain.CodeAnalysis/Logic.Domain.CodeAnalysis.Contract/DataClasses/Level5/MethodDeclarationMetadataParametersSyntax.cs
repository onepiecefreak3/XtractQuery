using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class MethodDeclarationMetadataParametersSyntax : SyntaxNode
{
    public SyntaxToken RelSmaller { get; private set; }
    public MethodDeclarationMetadataParameterListSyntax List { get; private set; }
    public SyntaxToken RelBigger { get; private set; }

    public override SyntaxLocation Location => RelSmaller.FullLocation;
    public override SyntaxSpan Span => new(RelSmaller.FullSpan.Position, RelBigger.FullSpan.EndPosition);

    public MethodDeclarationMetadataParametersSyntax(SyntaxToken relSmaller, MethodDeclarationMetadataParameterListSyntax list, SyntaxToken relBigger)
    {
        relSmaller.Parent = this;
        list.Parent = this;
        relBigger.Parent = this;

        RelSmaller = relSmaller;
        List = list;
        RelBigger = relBigger;

        Root.Update();
    }

    public void SetRelSmaller(SyntaxToken relSmaller, bool updatePosition = true)
    {
        relSmaller.Parent = this;
        RelSmaller = relSmaller;

        if (updatePosition)
            Root.Update();
    }

    public void SetList(MethodDeclarationMetadataParameterListSyntax list, bool updatePosition = true)
    {
        list.Parent = this;
        List = list;

        if (updatePosition)
            Root.Update();
    }

    public void SetRelBigger(SyntaxToken relBigger, bool updatePosition = true)
    {
        relBigger.Parent = this;
        RelBigger = relBigger;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken relSmaller = RelSmaller;
        SyntaxToken relBigger = RelBigger;

        position = relSmaller.UpdatePosition(position, ref line, ref column);
        position = List.UpdatePosition(position, ref line, ref column);
        position = relBigger.UpdatePosition(position, ref line, ref column);

        RelSmaller = relSmaller;
        RelBigger = relBigger;

        return position;
    }
}