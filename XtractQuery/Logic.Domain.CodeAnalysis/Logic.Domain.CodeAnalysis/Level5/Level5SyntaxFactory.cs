using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using System.Globalization;

namespace Logic.Domain.CodeAnalysis.Level5;

internal class Level5SyntaxFactory : ILevel5SyntaxFactory
{
    public SyntaxToken Create(string text, int rawKind, SyntaxTokenTrivia? leadingTrivia = null, SyntaxTokenTrivia? trailingTrivia = null)
    {
        return new(text, rawKind, leadingTrivia, trailingTrivia);
    }

    public SyntaxToken Token(SyntaxTokenKind kind)
    {
        switch (kind)
        {
            case SyntaxTokenKind.Comma: return new(",", (int)kind);
            case SyntaxTokenKind.Colon: return new(":", (int)kind);
            case SyntaxTokenKind.Semicolon: return new(";", (int)kind);
            case SyntaxTokenKind.Underscore: return new("_", (int)kind);
            case SyntaxTokenKind.EqualsSign: return new("=", (int)kind);
            case SyntaxTokenKind.Complement: return new("~", (int)kind);
            case SyntaxTokenKind.Minus: return new("-", (int)kind);
            case SyntaxTokenKind.Plus: return new("+", (int)kind);
            case SyntaxTokenKind.Mul: return new("*", (int)kind);
            case SyntaxTokenKind.Div: return new("/", (int)kind);
            case SyntaxTokenKind.Mod: return new("%", (int)kind);
            case SyntaxTokenKind.And: return new("&", (int)kind);
            case SyntaxTokenKind.Or: return new("|", (int)kind);
            case SyntaxTokenKind.Xor: return new("^", (int)kind);

            case SyntaxTokenKind.Equals: return new("==", (int)kind);
            case SyntaxTokenKind.NotEquals: return new("!=", (int)kind);
            case SyntaxTokenKind.SmallerEquals: return new("<=", (int)kind);
            case SyntaxTokenKind.GreaterEquals: return new(">=", (int)kind);
            case SyntaxTokenKind.PlusEquals: return new("+=", (int)kind);
            case SyntaxTokenKind.MinusEquals: return new("-=", (int)kind);
            case SyntaxTokenKind.MulEquals: return new("*=", (int)kind);
            case SyntaxTokenKind.DivEquals: return new("/=", (int)kind);
            case SyntaxTokenKind.ModEquals: return new("%=", (int)kind);
            case SyntaxTokenKind.AndEquals: return new("&=", (int)kind);
            case SyntaxTokenKind.OrEquals: return new("|=", (int)kind);
            case SyntaxTokenKind.XorEquals: return new("^=", (int)kind);
            case SyntaxTokenKind.LeftShiftEquals: return new("<<=", (int)kind);
            case SyntaxTokenKind.RightShiftEquals: return new(">>=", (int)kind);
            case SyntaxTokenKind.ArrowRight: return new("=>", (int)kind);
            case SyntaxTokenKind.Decrement: return new("--", (int)kind);
            case SyntaxTokenKind.Increment: return new("++", (int)kind);
            case SyntaxTokenKind.Smaller: return new("<", (int)kind);
            case SyntaxTokenKind.Greater: return new(">", (int)kind);
            case SyntaxTokenKind.LeftShift: return new("<<", (int)kind);
            case SyntaxTokenKind.RightShift: return new(">>", (int)kind);

            case SyntaxTokenKind.ParenOpen: return new("(", (int)kind);
            case SyntaxTokenKind.ParenClose: return new(")", (int)kind);
            case SyntaxTokenKind.CurlyOpen: return new("{", (int)kind);
            case SyntaxTokenKind.CurlyClose: return new("}", (int)kind);
            case SyntaxTokenKind.BracketOpen: return new("[", (int)kind);
            case SyntaxTokenKind.BracketClose: return new("]", (int)kind);

            case SyntaxTokenKind.YieldKeyword: return new("yield", (int)kind);
            case SyntaxTokenKind.ReturnKeyword: return new("return", (int)kind);
            case SyntaxTokenKind.ExitKeyword: return new("exit", (int)kind);
            case SyntaxTokenKind.NewKeyword: return new("new", (int)kind);
            case SyntaxTokenKind.NotKeyword: return new("not", (int)kind);
            case SyntaxTokenKind.OrKeyword: return new("or", (int)kind);
            case SyntaxTokenKind.AndKeyword: return new("and", (int)kind);
            case SyntaxTokenKind.SwitchKeyword: return new("switch", (int)kind);
            case SyntaxTokenKind.GotoKeyword: return new("goto", (int)kind);
            case SyntaxTokenKind.IfKeyword: return new("if", (int)kind);
            case SyntaxTokenKind.IntKeyword: return new("int", (int)kind);
            case SyntaxTokenKind.BoolKeyword: return new("bool", (int)kind);
            case SyntaxTokenKind.FloatKeyword: return new("float", (int)kind);
            default: throw new InvalidOperationException($"Cannot create simple token from kind {kind}. Use other methods instead.");
        }
    }

    public SyntaxToken NumericLiteral(long value)
    {
        return new($"{value}", (int)SyntaxTokenKind.NumericLiteral);
    }

    public SyntaxToken HashNumericLiteral(ulong value)
    {
        return new($"0x{value:X8}h", (int)SyntaxTokenKind.HashNumericLiteral);
    }

    public SyntaxToken HashStringLiteral(string text)
    {
        return new($"\"{text}\"h", (int)SyntaxTokenKind.HashStringLiteral);
    }

    public SyntaxToken FloatingNumericLiteral(float value)
    {
        return new($"{value.ToString(CultureInfo.GetCultureInfo("en-gb"))}f", (int)SyntaxTokenKind.FloatingNumericLiteral);
    }

    public SyntaxToken StringLiteral(string text)
    {
        return new($"\"{text.Replace("\"", "\\\"")}\"", (int)SyntaxTokenKind.StringLiteral);
    }

    public SyntaxToken Identifier(string text)
    {
        return new(text, (int)SyntaxTokenKind.Identifier);
    }

    public SyntaxToken Variable(string name, uint slot)
    {
        return new($"${name}{slot}", (int)SyntaxTokenKind.Variable);
    }
}