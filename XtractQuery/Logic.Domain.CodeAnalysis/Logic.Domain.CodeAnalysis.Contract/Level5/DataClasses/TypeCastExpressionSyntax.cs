using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses
{
    public class TypeCastExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken ParenOpen { get; private set; }
        public SyntaxToken TypeKeyword { get; private set; }
        public SyntaxToken ParenClose { get; private set; }

        public override SyntaxLocation Location => ParenOpen.FullLocation;
        public override SyntaxSpan Span => new(ParenOpen.FullSpan.Position, ParenClose.FullSpan.EndPosition);

        public TypeCastExpressionSyntax(SyntaxToken parenOpen, SyntaxToken type, SyntaxToken parenClose)
        {
            parenOpen.Parent = this;
            type.Parent = this;
            parenClose.Parent = this;

            ParenOpen = parenOpen;
            TypeKeyword = type;
            ParenClose = parenClose;

            Root.Update();
        }

        public void SetParenOpen(SyntaxToken parenOpen, bool updatePositions = true)
        {
            parenOpen.Parent = this;

            ParenOpen = parenOpen;

            if (updatePositions)
                Root.Update();
        }

        public void SetType(SyntaxToken type, bool updatePositions = true)
        {
            type.Parent = this;

            TypeKeyword = type;

            if (updatePositions)
                Root.Update();
        }

        public void SetParenClose(SyntaxToken parenClose, bool updatePositions = true)
        {
            parenClose.Parent = this;

            ParenClose = parenClose;

            if (updatePositions)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken parenOpen = ParenOpen;
            SyntaxToken type = TypeKeyword;
            SyntaxToken parenClose = ParenClose;

            position = parenOpen.UpdatePosition(position, ref line, ref column);
            position = type.UpdatePosition(position, ref line, ref column);
            position = parenClose.UpdatePosition(position, ref line, ref column);

            ParenOpen = parenOpen;
            TypeKeyword = type;
            ParenClose = parenClose;

            return position;
        }
    }
}
