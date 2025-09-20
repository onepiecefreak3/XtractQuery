using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;

public class MethodDeclarationSyntax : SyntaxNode
{
    public SyntaxToken Identifier { get; private set; }
    public MethodDeclarationMetadataParametersSyntax? MetadataParameters { get; private set; }
    public MethodDeclarationParametersSyntax Parameters { get; private set; }
    public MethodDeclarationBodySyntax Body { get; private set; }

    public override SyntaxLocation Location => Identifier.FullLocation;
    public override SyntaxSpan Span => new(Identifier.FullSpan.Position, Body.Span.EndPosition);

    public MethodDeclarationSyntax(SyntaxToken identifier, MethodDeclarationMetadataParametersSyntax? metadataParameters,
        MethodDeclarationParametersSyntax parameters, MethodDeclarationBodySyntax body)
    {
        identifier.Parent = this;
        if (metadataParameters != null)
            metadataParameters.Parent = this;
        parameters.Parent = this;
        body.Parent = this;

        Identifier = identifier;
        MetadataParameters = metadataParameters;
        Parameters = parameters;
        Body = body;

        Root.Update();
    }

    public void SetIdentifier(SyntaxToken identifier, bool updatePosition = true)
    {
        identifier.Parent = this;
        Identifier = identifier;

        if (updatePosition)
            Root.Update();
    }

    public void SetMetadataParameters(MethodDeclarationMetadataParametersSyntax? metadataParameters, bool updatePosition = true)
    {
        if (metadataParameters != null)
            metadataParameters.Parent = this;

        MetadataParameters = metadataParameters;

        if (updatePosition)
            Root.Update();
    }

    public void SetParameters(MethodDeclarationParametersSyntax parameters, bool updatePosition = true)
    {
        parameters.Parent = this;
        Parameters = parameters;

        if (updatePosition)
            Root.Update();
    }

    public void SetBody(MethodDeclarationBodySyntax body, bool updatePosition = true)
    {
        body.Parent = this;
        Body = body;

        if (updatePosition)
            Root.Update();
    }

    internal override int UpdatePosition(int position, ref int line, ref int column)
    {
        SyntaxToken identifier = Identifier;

        position = identifier.UpdatePosition(position, ref line, ref column);
        if (MetadataParameters != null)
            position = MetadataParameters.UpdatePosition(position, ref line, ref column);
        position = Parameters.UpdatePosition(position, ref line, ref column);
        position = Body.UpdatePosition(position, ref line, ref column);

        Identifier = identifier;

        return position;
    }
}