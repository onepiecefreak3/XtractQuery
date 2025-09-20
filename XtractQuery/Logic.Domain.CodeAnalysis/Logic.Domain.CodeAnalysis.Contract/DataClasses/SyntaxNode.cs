namespace Logic.Domain.CodeAnalysis.Contract.DataClasses;

public abstract class SyntaxNode
{
    public SyntaxNode? Parent { get; internal set; }
    public SyntaxNode Root => Parent?.Root ?? this;
        
    public abstract SyntaxLocation Location { get; }
    public abstract SyntaxSpan Span { get; }

    public void Update()
    {
        var line = 1;
        var column = 1;
        UpdatePosition(0, ref line, ref column);
    }

    internal abstract int UpdatePosition(int position, ref int line, ref int column);
}