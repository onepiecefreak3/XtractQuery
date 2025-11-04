using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Domain.CodeAnalysis.DataClasses.Level5;

public struct Level5SyntaxToken
{
    public SyntaxTokenKind Kind { get; }
    public string Text { get; }

    public int Position { get; }
    public int Line { get; }
    public int Column { get; }

    public Level5SyntaxToken(SyntaxTokenKind kind, int position, int line, int column, string? text = null)
    {
        Text = text ?? string.Empty;
        Kind = kind;
        Position = position;
        Line = line;
        Column = column;
    }
}