using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.CodeAnalysis.Contract.Exceptions;

namespace Logic.Domain.CodeAnalysis.Contract;

[MapException(typeof(TokenFactoryException))]
public interface ITokenFactory<TToken>
    where TToken : struct
{
    ILexer<TToken> CreateLexer(string text);
    IBuffer<TToken> CreateTokenBuffer(ILexer<TToken> lexer);
}