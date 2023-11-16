using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses
{
    public class ArrayIndexerExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken BracketOpen { get; private set; }
        public ValueExpressionSyntax Index { get; private set; }
        public SyntaxToken BracketClose { get; private set; }

        public override SyntaxLocation Location => BracketOpen.FullLocation;
        public override SyntaxSpan Span => new(BracketOpen.FullSpan.Position, BracketClose.FullSpan.EndPosition);

        public ArrayIndexerExpressionSyntax(SyntaxToken bracketOpen, ValueExpressionSyntax index, SyntaxToken bracketClose)
        {
            bracketOpen.Parent = this;
            index.Parent = this;
            bracketClose.Parent = this;

            BracketOpen = bracketOpen;
            Index = index;
            BracketClose = bracketClose;

            Root.Update();
        }

        public void SetBracketOpen(SyntaxToken bracketOpen, bool updatePositions = true)
        {
            bracketOpen.Parent = this;

            BracketOpen = bracketOpen;

            if (updatePositions)
                Root.Update();
        }

        public void SetBracketClose(SyntaxToken bracketClose, bool updatePositions = true)
        {
            bracketClose.Parent = this;

            BracketClose = bracketClose;

            if (updatePositions)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken bracketOpen = BracketOpen;
            SyntaxToken bracketClose = BracketClose;

            position = bracketOpen.UpdatePosition(position, ref line, ref column);
            position = Index.UpdatePosition(position, ref line, ref column);
            position = bracketClose.UpdatePosition(position, ref line, ref column);

            BracketOpen = bracketOpen;
            BracketClose = bracketClose;

            return position;
        }
    }
}
