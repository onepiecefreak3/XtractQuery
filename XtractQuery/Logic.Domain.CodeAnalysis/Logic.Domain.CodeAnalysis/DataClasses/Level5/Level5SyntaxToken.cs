using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;

namespace Logic.Domain.CodeAnalysis.DataClasses.Level5;

public struct Level5SyntaxToken(SyntaxTokenKind kind, int position, int line, int column, string? text = null)
{
    public SyntaxTokenKind Kind { get; } = kind;
    public string Text { get; } = text ?? string.Empty;

    public int Position { get; } = position;
    public int Line { get; } = line;
    public int Column { get; } = column;
}