using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses
{
    public class YieldStatementSyntax : StatementSyntax
    {
        public SyntaxToken Yield { get; private set; }
        public SyntaxToken Semicolon { get; private set; }

        public override SyntaxLocation Location => Yield.FullLocation;
        public override SyntaxSpan Span => new(Yield.FullSpan.Position, Semicolon.FullSpan.EndPosition);

        public YieldStatementSyntax(SyntaxToken yield, SyntaxToken semicolon)
        {
            yield.Parent = this;
            semicolon.Parent = this;

            Yield = yield;
            Semicolon = semicolon;

            Root.Update();
        }

        public void SetYield(SyntaxToken yield, bool updatePositions = true)
        {
            yield.Parent = this;

            Yield = yield;

            if (updatePositions)
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
            SyntaxToken yield = Yield;
            SyntaxToken semicolon = Semicolon;

            position = yield.UpdatePosition(position, ref line, ref column);
            position = semicolon.UpdatePosition(position, ref line, ref column);

            Yield = yield;
            Semicolon = semicolon;

            return position;
        }
    }
}
