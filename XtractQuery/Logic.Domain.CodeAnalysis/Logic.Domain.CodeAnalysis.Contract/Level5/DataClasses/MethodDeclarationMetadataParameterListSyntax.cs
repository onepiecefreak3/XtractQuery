using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses
{
    public class MethodDeclarationMetadataParameterListSyntax : SyntaxNode
    {
        public LiteralExpressionSyntax Parameter1 { get; private set; }
        public SyntaxToken Comma { get; private set; }
        public LiteralExpressionSyntax Parameter2 { get; private set; }

        public override SyntaxLocation Location => Parameter1.Location;
        public override SyntaxSpan Span => new(Parameter1.Span.Position, Parameter2.Span.EndPosition);

        public MethodDeclarationMetadataParameterListSyntax(LiteralExpressionSyntax parameter1, SyntaxToken comma, LiteralExpressionSyntax parameter2)
        {
            parameter1.Parent = this;
            comma.Parent = this;
            parameter2.Parent = this;

            Parameter1 = parameter1;
            Comma = comma;
            Parameter2 = parameter2;

            Root.Update();
        }

        public void SetParameter1(LiteralExpressionSyntax parameter, bool updatePosition = true)
        {
            parameter.Parent = this;
            Parameter1 = parameter;

            if (updatePosition)
                Root.Update();
        }

        public void SetComma(SyntaxToken comma, bool updatePosition = true)
        {
            comma.Parent = this;
            Comma = comma;

            if (updatePosition)
                Root.Update();
        }

        public void SetParameter2(LiteralExpressionSyntax parameter, bool updatePosition = true)
        {
            parameter.Parent = this;
            Parameter2 = parameter;

            if (updatePosition)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken comma = Comma;

            position = Parameter1.UpdatePosition(position, ref line, ref column);
            position = comma.UpdatePosition(position, ref line, ref column);
            position = Parameter2.UpdatePosition(position, ref line, ref column);

            Comma = comma;

            return position;
        }
    }
}
