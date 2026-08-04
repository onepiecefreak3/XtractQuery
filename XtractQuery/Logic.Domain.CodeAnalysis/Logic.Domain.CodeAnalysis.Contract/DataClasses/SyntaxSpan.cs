namespace Logic.Domain.CodeAnalysis.Contract.DataClasses;

public readonly struct SyntaxSpan(int position, int endPosition)
{
    public int Position { get; } = position;
    public int EndPosition { get; } = endPosition;

    public override string ToString()
    {
        return $"[{Position}..{EndPosition})";
    }
}