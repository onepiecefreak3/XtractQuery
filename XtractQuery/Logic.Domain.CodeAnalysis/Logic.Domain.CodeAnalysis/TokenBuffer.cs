using Logic.Domain.CodeAnalysis.Contract;

namespace Logic.Domain.CodeAnalysis;

internal class TokenBuffer<TToken>(ILexer<TToken> lexer) : Buffer<TToken>
    where TToken : struct
{
    public override bool IsEndOfInput { get; protected set; }

    protected override TToken ReadInternal()
    {
        TToken value = lexer.Read();
        IsEndOfInput = lexer.IsEndOfInput;

        return value;
    }
}