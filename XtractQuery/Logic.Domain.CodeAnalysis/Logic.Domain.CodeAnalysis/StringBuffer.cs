namespace Logic.Domain.CodeAnalysis;

internal class StringBuffer(string text) : Buffer<int>
{
    private readonly TextReader _reader = new StringReader(text);

    public override bool IsEndOfInput { get; protected set; }

    protected override int ReadInternal()
    {
        int value = _reader.Read();
        IsEndOfInput = value < 0;

        return value;
    }
}