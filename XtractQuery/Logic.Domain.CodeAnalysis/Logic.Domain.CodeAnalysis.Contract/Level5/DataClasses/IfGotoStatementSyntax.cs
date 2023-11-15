using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses
{
    public class IfGotoStatementSyntax : StatementSyntax
    {
        public SyntaxToken If { get; private set; }
        public ValueExpressionSyntax Value { get; private set; }
        public GotoStatementSyntax Goto { get; private set; }

        public override SyntaxLocation Location => If.FullLocation;
        public override SyntaxSpan Span => new(If.FullSpan.Position, Goto.Span.EndPosition);

        public IfGotoStatementSyntax(SyntaxToken ifToken, ValueExpressionSyntax value, GotoStatementSyntax gotoStatement)
        {
            ifToken.Parent = this;
            value.Parent = this;
            gotoStatement.Parent = this;

            If = ifToken;
            Value = value;
            Goto = gotoStatement;

            Root.Update();
        }

        public void SetIf(SyntaxToken ifToken, bool updatePositions = true)
        {
            ifToken.Parent = this;

            If = ifToken;

            if (updatePositions)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken ifToken = If;

            position = ifToken.UpdatePosition(position, ref line, ref column);
            position = Value.UpdatePosition(position, ref line, ref column);
            position = Goto.UpdatePosition(position, ref line, ref column);

            If = ifToken;

            return position;
        }
    }
}
