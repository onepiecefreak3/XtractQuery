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
    public class MethodInvocationExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken Identifier { get; private set; }
        public MethodInvocationExpressionParametersSyntax Parameters { get; private set; }

        public override SyntaxLocation Location => Identifier.FullLocation;
        public override SyntaxSpan Span => new(Identifier.FullSpan.Position, Parameters.Span.EndPosition);

        public MethodInvocationExpressionSyntax(SyntaxToken identifier, MethodInvocationExpressionParametersSyntax parameters)
        {
            identifier.Parent = this;
            parameters.Parent = this;

            Identifier = identifier;
            Parameters = parameters;

            Root.Update();
        }

        public void SetIdentifier(SyntaxToken identifier, bool updatePosition = true)
        {
            identifier.Parent = this;
            Identifier = identifier;

            if (updatePosition)
                Root.Update();
        }

        public void SetParameters(MethodInvocationExpressionParametersSyntax parameters, bool updatePosition = true)
        {
            parameters.Parent = this;
            Parameters = parameters;

            if (updatePosition)
                Root.Update();
        }


        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken identifier = Identifier;

            position = identifier.UpdatePosition(position, ref line, ref column);
            position = Parameters.UpdatePosition(position, ref line, ref column);

            Identifier = identifier;

            return position;
        }
    }
}
