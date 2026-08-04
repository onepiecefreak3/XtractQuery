namespace Logic.Domain.CodeAnalysis.Contract.DataClasses;

public struct SyntaxLocation(int line, int column)
{
    public int Line { get; } = line;
    public int Column { get; } = column;

    public override string ToString()
    {
        return $"({Line}, {Column})";
    }
}