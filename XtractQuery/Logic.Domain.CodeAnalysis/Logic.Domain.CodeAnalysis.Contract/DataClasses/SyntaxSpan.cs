namespace Logic.Domain.CodeAnalysis.Contract.DataClasses;

public readonly struct SyntaxSpan
{
    public int Position { get; }
    public int EndPosition { get; }

    public SyntaxSpan(int position, int endPosition)
    {
        Position = position;
        EndPosition = endPosition;
    }

    public override string ToString()
    {
        return $"[{Position}..{EndPosition})";
    }
}