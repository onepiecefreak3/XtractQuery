using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class ArrayInstantiationExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken New { get; private set; }
    public IReadOnlyList<ArrayIndexerExpressionSyntax> Indexer { get; private set; }

    public override SyntaxLocation Location => New.FullLocation;
    public override SyntaxSpan Span => new(New.FullSpan.Position, Indexer.Count <= 0 ? New.FullSpan.EndPosition : Indexer[^1].Span.EndPosition);

    public ArrayInstantiationExpressionSyntax(SyntaxToken newToken, IReadOnlyList<ArrayIndexerExpressionSyntax> indexer)
    {
        newToken.Parent = this;
        foreach (var index in indexer)
            index.Parent = this;

        New = newToken;
        Indexer = indexer;

        Root.Update();
    }

    public void SetNew(SyntaxToken newToken, bool updatePositions = true)
    {
        newToken.Parent = this;

        New = newToken;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken newToken = New;

        position = newToken.UpdatePosition(position, ref line, ref column);
        foreach (var index in Indexer)
            position = index.UpdatePosition(position, ref line, ref column);

        New = newToken;

        return position;
    }
}