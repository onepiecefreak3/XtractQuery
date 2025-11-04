using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

public class CommaSeparatedSyntaxList<TNode> : SyntaxNode
    where TNode : SyntaxNode
{
    public IReadOnlyList<TNode> Elements { get; private set; }

    public override SyntaxLocation Location => Elements.Count > 0 ? Elements[0].Location : new(1, 1);
    public override SyntaxSpan Span => Elements.Count > 0 ? new(Elements[0].Span.Position, Elements[^1].Span.EndPosition) : new();

    public CommaSeparatedSyntaxList(IReadOnlyList<TNode>? elements)
    {
        Elements = elements ?? new List<TNode>();

        foreach (TNode parameter in Elements)
            parameter.Parent = this;

        Root.Update();
    }

    public void SetElements(IReadOnlyList<TNode>? elements, bool updatePosition = true)
    {
        Elements = elements ?? new List<TNode>();

        foreach (TNode parameter in Elements)
            parameter.Parent = this;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        foreach (TNode parameter in Elements)
        {
            position = parameter.UpdatePosition(position, ref line, ref column);

            if (parameter != Elements[^1])
                position++;
        }

        return position;
    }
}