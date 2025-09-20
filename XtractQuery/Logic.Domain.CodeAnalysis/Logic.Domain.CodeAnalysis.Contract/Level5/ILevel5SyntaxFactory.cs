using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.CodeAnalysis.Contract.Level5.Exceptions;

namespace Logic.Domain.CodeAnalysis.Contract.Level5;

[MapException(typeof(Level5SyntaxFactoryException))]
public interface ILevel5SyntaxFactory
{
    SyntaxToken Create(string text, int rawKind, SyntaxTokenTrivia? leadingTrivia = null, SyntaxTokenTrivia? trailingTrivia = null);

    SyntaxToken Token(SyntaxTokenKind kind);

    SyntaxToken NumericLiteral(long value);
    SyntaxToken HashNumericLiteral(ulong value);
    SyntaxToken HashStringLiteral(string text);
    SyntaxToken FloatingNumericLiteral(float value);
    SyntaxToken StringLiteral(string text);
    SyntaxToken Identifier(string text);
    SyntaxToken Variable(string name, uint slot);
}