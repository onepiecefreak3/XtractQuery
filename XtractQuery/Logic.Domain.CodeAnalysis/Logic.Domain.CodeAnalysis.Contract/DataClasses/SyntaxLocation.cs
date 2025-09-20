namespace Logic.Domain.CodeAnalysis.Contract.DataClasses;

public struct SyntaxLocation
{
    public int Line { get; }
    public int Column { get; }

    public SyntaxLocation(int line, int column)
    {
        Line = line;
        Column = column;
    }

    public override string ToString()
    {
        return $"({Line}, {Column})";
    }
}