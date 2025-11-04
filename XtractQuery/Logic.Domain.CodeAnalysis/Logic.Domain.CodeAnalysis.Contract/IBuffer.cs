namespace Logic.Domain.CodeAnalysis.Contract;

public interface IBuffer<out T>
{
    bool IsEndOfInput { get; }

    T Peek(int position = 0);

    T Read();
}