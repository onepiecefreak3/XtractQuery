namespace Logic.Domain.CodeAnalysis.Contract;

public interface ITokenFactory<TToken>
    where TToken : struct
{
    ILexer<TToken> CreateLexer(string text);
    IBuffer<TToken> CreateTokenBuffer(ILexer<TToken> lexer);
}