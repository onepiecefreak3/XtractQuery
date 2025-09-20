using System.Text;
using Logic.Domain.CodeAnalysis.Contract;
using Logic.Domain.CodeAnalysis.Contract.Exceptions;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.CodeAnalysis.Level5.InternalContract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Level5;

internal class Level5ScriptLexer : ILexer<Level5SyntaxToken>
{
    private readonly StringBuilder _sb;
    private readonly IBuffer<int> _buffer;

    public bool IsEndOfInput => _buffer.IsEndOfInput;

    private int Line { get; set; } = 1;
    private int Column { get; set; } = 1;
    private int Position { get; set; }

    public Level5ScriptLexer(IBuffer<int> buffer)
    {
        _sb = new StringBuilder();
        _buffer = buffer;
    }

    public Level5SyntaxToken Read()
    {
        if (!TryPeekChar(out char character))
            return new Level5SyntaxToken(SyntaxTokenKind.EndOfFile, Position, Line, Column);

        switch (character)
        {
            case ',':
                return new Level5SyntaxToken(SyntaxTokenKind.Comma, Position, Line, Column, $"{ReadChar()}");
            case ':':
                return new Level5SyntaxToken(SyntaxTokenKind.Colon, Position, Line, Column, $"{ReadChar()}");
            case ';':
                return new Level5SyntaxToken(SyntaxTokenKind.Semicolon, Position, Line, Column, $"{ReadChar()}");
            case '=':
                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.Equals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                if (IsPeekedChar(1, '>'))
                    return new Level5SyntaxToken(SyntaxTokenKind.ArrowRight, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new Level5SyntaxToken(SyntaxTokenKind.EqualsSign, Position, Line, Column, $"{ReadChar()}");

            case '~':
                return new Level5SyntaxToken(SyntaxTokenKind.Complement, Position, Line, Column, $"{ReadChar()}");
            case '!':
                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.NotEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                break;

            case '(':
                return new Level5SyntaxToken(SyntaxTokenKind.ParenOpen, Position, Line, Column, $"{ReadChar()}");
            case ')':
                return new Level5SyntaxToken(SyntaxTokenKind.ParenClose, Position, Line, Column, $"{ReadChar()}");
            case '{':
                return new Level5SyntaxToken(SyntaxTokenKind.CurlyOpen, Position, Line, Column, $"{ReadChar()}");
            case '}':
                return new Level5SyntaxToken(SyntaxTokenKind.CurlyClose, Position, Line, Column, $"{ReadChar()}");
            case '[':
                return new Level5SyntaxToken(SyntaxTokenKind.BracketOpen, Position, Line, Column, $"{ReadChar()}");
            case ']':
                return new Level5SyntaxToken(SyntaxTokenKind.BracketClose, Position, Line, Column, $"{ReadChar()}");
            case '<':
                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.SmallerEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                if (IsPeekedChar(1, '<'))
                {
                    if (IsPeekedChar(2, '='))
                        return new Level5SyntaxToken(SyntaxTokenKind.LeftShiftEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}{ReadChar()}");

                    return new Level5SyntaxToken(SyntaxTokenKind.LeftShift, Position, Line, Column, $"{ReadChar()}{ReadChar()}");
                }

                return new Level5SyntaxToken(SyntaxTokenKind.Smaller, Position, Line, Column, $"{ReadChar()}");

            case '>':
                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.GreaterEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                if (IsPeekedChar(1, '>'))
                {
                    if (IsPeekedChar(2, '='))
                        return new Level5SyntaxToken(SyntaxTokenKind.RightShiftEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}{ReadChar()}");

                    return new Level5SyntaxToken(SyntaxTokenKind.RightShift, Position, Line, Column, $"{ReadChar()}{ReadChar()}");
                }

                return new Level5SyntaxToken(SyntaxTokenKind.Greater, Position, Line, Column, $"{ReadChar()}");

            case '*':
                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.MulEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new Level5SyntaxToken(SyntaxTokenKind.Mul, Position, Line, Column, $"{ReadChar()}");

            case '/':
                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.DivEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                if (!IsPeekedChar(1, '/'))
                    return new Level5SyntaxToken(SyntaxTokenKind.Div, Position, Line, Column, $"{ReadChar()}");

                goto case ' ';

            case '%':
                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.ModEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new Level5SyntaxToken(SyntaxTokenKind.Mod, Position, Line, Column, $"{ReadChar()}");

            case '&':
                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.AndEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new Level5SyntaxToken(SyntaxTokenKind.And, Position, Line, Column, $"{ReadChar()}");

            case '|':
                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.OrEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new Level5SyntaxToken(SyntaxTokenKind.Or, Position, Line, Column, $"{ReadChar()}");

            case '^':
                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.XorEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new Level5SyntaxToken(SyntaxTokenKind.Xor, Position, Line, Column, $"{ReadChar()}");

            case '-':
                if (IsPeekedChar(1, '.') || (TryPeekChar(1, out character) && character is >= '0' and <= '9'))
                    goto case '.';

                if (IsPeekedChar(1, '-'))
                    return new Level5SyntaxToken(SyntaxTokenKind.Decrement, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.MinusEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new Level5SyntaxToken(SyntaxTokenKind.Minus, Position, Line, Column, $"{ReadChar()}");

            case '+':
                if (IsPeekedChar(1, '+'))
                    return new Level5SyntaxToken(SyntaxTokenKind.Increment, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                if (IsPeekedChar(1, '='))
                    return new Level5SyntaxToken(SyntaxTokenKind.PlusEquals, Position, Line, Column, $"{ReadChar()}{ReadChar()}");

                return new Level5SyntaxToken(SyntaxTokenKind.Plus, Position, Line, Column, $"{ReadChar()}");

            case ' ':
            case '\t':
            case '\r':
            case '\n':
                return ReadTriviaAndComments();

            case '"':
                return ReadStringLiteral();

            case '.':
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                return ReadNumericLiteral();

            case 'a':
            case 'b':
            case 'c':
            case 'd':
            case 'e':
            case 'f':
            case 'g':
            case 'h':
            case 'i':
            case 'j':
            case 'k':
            case 'l':
            case 'm':
            case 'n':
            case 'o':
            case 'p':
            case 'q':
            case 'r':
            case 's':
            case 't':
            case 'u':
            case 'v':
            case 'w':
            case 'x':
            case 'y':
            case 'z':
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            case 'E':
            case 'F':
            case 'G':
            case 'H':
            case 'I':
            case 'J':
            case 'K':
            case 'L':
            case 'M':
            case 'N':
            case 'O':
            case 'P':
            case 'Q':
            case 'R':
            case 'S':
            case 'T':
            case 'U':
            case 'V':
            case 'W':
            case 'X':
            case 'Y':
            case 'Z':
            case '_':
            case '@':
                return ReadIdentifierOrKeyword();

            case '$':
                return ReadVariable();
        }

        throw CreateException("Invalid character.");
    }

    private Level5SyntaxToken ReadTriviaAndComments()
    {
        int position = Position;
        int line = Line;
        int column = Column;

        _sb.Clear();

        while (TryPeekChar(out char character))
        {
            switch (character)
            {
                case '/':
                    if (IsPeekedChar(1, '/'))
                    {
                        _sb.Append(ReadChar());
                        _sb.Append(ReadChar());

                        while (!IsPeekedChar('\n'))
                            _sb.Append(ReadChar());

                        continue;
                    }

                    if (IsPeekedChar(1, '*'))
                    {
                        _sb.Append(ReadChar());
                        _sb.Append(ReadChar());

                        while (!IsPeekedChar('*') || !IsPeekedChar(1, '/'))
                            _sb.Append(ReadChar());

                        _sb.Append(ReadChar());
                        _sb.Append(ReadChar());

                        continue;
                    }

                    break;

                case ' ':
                case '\t':
                case '\r':
                case '\n':
                    _sb.Append(ReadChar());
                    continue;
            }

            break;
        }

        return new Level5SyntaxToken(SyntaxTokenKind.Trivia, position, line, column, _sb.ToString());
    }

    private Level5SyntaxToken ReadStringLiteral()
    {
        int position = Position;
        int line = Line;
        int column = Column;

        _sb.Clear();

        if (!IsPeekedChar('"'))
            throw CreateException("Invalid string literal start.", "\"");

        _sb.Append(ReadChar());

        while (!IsPeekedChar('"'))
        {
            if (IsPeekedChar('\\'))
                _sb.Append(ReadChar());

            _sb.Append(ReadChar());
        }

        if (_buffer.IsEndOfInput)
            throw CreateException("Invalid string literal end.", "\"");

        _sb.Append(ReadChar());

        if (!IsPeekedChar('h'))
            return new Level5SyntaxToken(SyntaxTokenKind.StringLiteral, position, line, column, _sb.ToString());

        _sb.Append(ReadChar());
        return new Level5SyntaxToken(SyntaxTokenKind.HashStringLiteral, position, line, column, _sb.ToString());

    }

    private Level5SyntaxToken ReadNumericLiteral()
    {
        int position = Position;
        int line = Line;
        int column = Column;

        _sb.Clear();

        var isHex = false;
        var hasDot = false;
        int dotColumn = Column;
        var kind = SyntaxTokenKind.NumericLiteral;

        while (TryPeekChar(out char character))
        {
            switch (character)
            {
                case '0':
                    if (!IsPeekedChar(1, 'x'))
                        goto case '1';

                    if (_sb.Length != 0)
                        throw CreateException($"Invalid hex identifier in numeric literal {character} in numeric literal.");

                    _sb.Append(ReadChar());
                    _sb.Append(ReadChar());

                    isHex = true;
                    continue;

                case '-':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    _sb.Append(ReadChar());
                    continue;

                case 'E':
                    _sb.Append(ReadChar());
                    continue;

                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'F':
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                    if (!isHex)
                        throw CreateException("Invalid character in numeric literal.");

                    _sb.Append(ReadChar());
                    continue;

                case '.':
                    if (hasDot)
                        throw CreateException("Invalid floating numeric literal with multiple dots.");

                    hasDot = true;
                    dotColumn = Column;

                    _sb.Append(ReadChar());
                    continue;

                case 'f':
                    if (isHex)
                        goto case 'A';

                    if (hasDot && dotColumn == Column - 1)
                        throw CreateException("Floating numeric value misses fractional part.");

                    kind = SyntaxTokenKind.FloatingNumericLiteral;

                    _sb.Append(ReadChar());
                    break;

                case 'h':
                    if (hasDot)
                        throw CreateException("Floating numeric literal marked as hash numeric literal ('h').");

                    kind = SyntaxTokenKind.HashNumericLiteral;

                    _sb.Append(ReadChar());
                    break;
            }

            break;
        }

        if (hasDot && kind != SyntaxTokenKind.FloatingNumericLiteral)
            kind = SyntaxTokenKind.FloatingNumericLiteral;

        if (hasDot && dotColumn == Column - 1)
            throw CreateException("Floating numeric value misses fractional part.");

        return new Level5SyntaxToken(kind, position, line, column, _sb.ToString());
    }

    private Level5SyntaxToken ReadIdentifierOrKeyword()
    {
        int position = Position;
        int line = Line;
        int column = Column;

        _sb.Clear();

        var firstChar = true;
        while (TryPeekChar(out char character))
        {
            switch (character)
            {
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                case 'g':
                case 'h':
                case 'i':
                case 'j':
                case 'k':
                case 'l':
                case 'm':
                case 'n':
                case 'o':
                case 'p':
                case 'q':
                case 'r':
                case 's':
                case 't':
                case 'u':
                case 'v':
                case 'w':
                case 'x':
                case 'y':
                case 'z':
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                case 'G':
                case 'H':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case '_':
                    firstChar = false;

                    _sb.Append(ReadChar());
                    continue;

                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    if (firstChar)
                        throw CreateException("Invalid identifier starting with numbers.");

                    firstChar = false;

                    _sb.Append(ReadChar());
                    continue;
            }

            if (firstChar)
                throw CreateException("Invalid identifier.");

            break;
        }

        var finalValue = _sb.ToString();
        switch (finalValue)
        {
            case "_":
                return new Level5SyntaxToken(SyntaxTokenKind.Underscore, position, line, column, finalValue);

            case "yield":
                return new Level5SyntaxToken(SyntaxTokenKind.YieldKeyword, position, line, column, finalValue);

            case "return":
                return new Level5SyntaxToken(SyntaxTokenKind.ReturnKeyword, position, line, column, finalValue);

            case "exit":
                return new Level5SyntaxToken(SyntaxTokenKind.ExitKeyword, position, line, column, finalValue);

            case "new":
                return new Level5SyntaxToken(SyntaxTokenKind.NewKeyword, position, line, column, finalValue);

            case "not":
                return new Level5SyntaxToken(SyntaxTokenKind.NotKeyword, position, line, column, finalValue);

            case "and":
                return new Level5SyntaxToken(SyntaxTokenKind.AndKeyword, position, line, column, finalValue);

            case "or":
                return new Level5SyntaxToken(SyntaxTokenKind.OrKeyword, position, line, column, finalValue);

            case "switch":
                return new Level5SyntaxToken(SyntaxTokenKind.SwitchKeyword, position, line, column, finalValue);

            case "goto":
                return new Level5SyntaxToken(SyntaxTokenKind.GotoKeyword, position, line, column, finalValue);

            case "if":
                return new Level5SyntaxToken(SyntaxTokenKind.IfKeyword, position, line, column, finalValue);

            case "int":
                return new Level5SyntaxToken(SyntaxTokenKind.IntKeyword, position, line, column, finalValue);

            case "bool":
                return new Level5SyntaxToken(SyntaxTokenKind.BoolKeyword, position, line, column, finalValue);

            case "float":
                return new Level5SyntaxToken(SyntaxTokenKind.FloatKeyword, position, line, column, finalValue);

            default:
                return new Level5SyntaxToken(SyntaxTokenKind.Identifier, position, line, column, finalValue);
        }
    }

    private Level5SyntaxToken ReadVariable()
    {
        int position = Position;
        int line = Line;
        int column = Column;

        if (!IsPeekedChar('$'))
            throw CreateException("Invalid variable.", "$");

        ReadChar();

        Level5SyntaxToken identifier = ReadIdentifierOrKeyword();

        return new Level5SyntaxToken(SyntaxTokenKind.Variable, position, line, column, $"${identifier.Text}");
    }

    private bool IsPeekedChar(char expected)
    {
        return IsPeekedChar(0, expected);
    }

    private bool IsPeekedChar(int position, char expected)
    {
        return TryPeekChar(position, out char character) && character == expected;
    }

    private bool TryPeekChar(out char character)
    {
        return TryPeekChar(0, out character);
    }

    private bool TryPeekChar(int position, out char character)
    {
        character = default;

        int result = _buffer.Peek(position);
        if (result < 0)
            return false;

        character = (char)result;
        return true;
    }

    private char ReadChar()
    {
        int result = _buffer.Read();
        if (result < 0)
            throw CreateException("Could not read character.");

        if (result == '\n')
        {
            Line++;
            Column = 0;
        }

        if (result == '\t')
            Column += 3;

        Column++;
        Position++;

        return (char)result;
    }

    private Exception CreateException(string message, string? expected = null)
    {
        message = $"{message} (Line {Line}, Column {Column})";

        if (!string.IsNullOrEmpty(expected))
            message = $"{message} (Expected \"{expected}\")";

        throw new LexerException(message, Line, Column);
    }
}