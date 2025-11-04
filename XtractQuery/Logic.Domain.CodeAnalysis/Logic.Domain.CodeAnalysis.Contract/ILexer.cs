namespace Logic.Domain.CodeAnalysis.Contract;

public interface ILexer<out TToken> where TToken : struct
{
    bool IsEndOfInput { get; }

    TToken Read();
}