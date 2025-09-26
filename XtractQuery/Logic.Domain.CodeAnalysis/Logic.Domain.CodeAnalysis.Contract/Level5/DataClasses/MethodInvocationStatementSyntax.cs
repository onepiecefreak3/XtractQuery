using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class MethodInvocationStatementSyntax : StatementSyntax
{
    public SyntaxToken Identifier { get; private set; }
    public MethodInvocationMetadataSyntax? Metadata { get; private set; }
    public MethodInvocationParametersSyntax Parameters { get; private set; }
    public SyntaxToken Semicolon { get; private set; }

    public override SyntaxLocation Location => Identifier.FullLocation;
    public override SyntaxSpan Span => new(Identifier.FullSpan.Position, Parameters.Span.EndPosition);

    public MethodInvocationStatementSyntax(SyntaxToken identifier, MethodInvocationMetadataSyntax? metadata, MethodInvocationParametersSyntax parameters, SyntaxToken semicolon)
    {
        identifier.Parent = this;
        if (metadata != null)
            metadata.Parent = this;
        parameters.Parent = this;
        semicolon.Parent = this;

        Identifier = identifier;
        Metadata = metadata;
        Parameters = parameters;
        Semicolon = semicolon;

        Root.Update();
    }

    public void SetIdentifier(SyntaxToken identifier, bool updatePosition = true)
    {
        identifier.Parent = this;
        Identifier = identifier;

        if (updatePosition)
            Root.Update();
    }

    public void SetMetadata(MethodInvocationMetadataSyntax? metadata, bool updatePosition = true)
    {
        if (metadata != null)
            metadata.Parent = this;
        Metadata = metadata;

        if (updatePosition)
            Root.Update();
    }

    public void SetParameters(MethodInvocationParametersSyntax parameters, bool updatePosition = true)
    {
        parameters.Parent = this;
        Parameters = parameters;

        if (updatePosition)
            Root.Update();
    }

    public void SetSemicolon(SyntaxToken semicolon, bool updatePositions = true)
    {
        semicolon.Parent = this;

        Semicolon = semicolon;

        if (updatePositions)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken identifier = Identifier;
        SyntaxToken semicolon = Semicolon;

        position = identifier.UpdatePosition(position, ref line, ref column);
        if (Metadata != null)
            position = Metadata.UpdatePosition(position, ref line, ref column);
        position = Parameters.UpdatePosition(position, ref line, ref column);
        position = semicolon.UpdatePosition(position, ref line, ref column);

        Identifier = identifier;
        Semicolon = semicolon;

        return position;
    }
}