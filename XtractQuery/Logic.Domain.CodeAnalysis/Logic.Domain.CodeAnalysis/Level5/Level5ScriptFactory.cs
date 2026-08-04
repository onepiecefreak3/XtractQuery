using Logic.Domain.CodeAnalysis.Contract;
using Logic.Domain.CodeAnalysis.DataClasses.Level5;

namespace Logic.Domain.CodeAnalysis.Level5;

internal class Level5ScriptFactory : ITokenFactory<Level5SyntaxToken>
{
    public ILexer<Level5SyntaxToken> CreateLexer(string text)
    {
        var buffer = new StringBuffer(text);
        return new Level5ScriptLexer(buffer);
    }

    public IBuffer<Level5SyntaxToken> CreateTokenBuffer(ILexer<Level5SyntaxToken> lexer)
    {
        return new TokenBuffer<Level5SyntaxToken>(lexer);
    }
}
