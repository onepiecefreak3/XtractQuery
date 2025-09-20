using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.CodeAnalysis.Contract.Exceptions;

namespace Logic.Domain.CodeAnalysis.Contract;

[MapException(typeof(LexerException))]
public interface ILexer<out TToken> where TToken : struct
{
    bool IsEndOfInput { get; }

    TToken Read();
}