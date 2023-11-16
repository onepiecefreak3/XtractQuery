using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses
{
    public class IfNotGotoStatementSyntax : StatementSyntax
    {
        public SyntaxToken If { get; private set; }
        public UnaryExpressionSyntax Comparison { get; private set; }
        public GotoStatementSyntax Goto { get; private set; }

        public override SyntaxLocation Location => If.FullLocation;
        public override SyntaxSpan Span => new(If.FullSpan.Position, Goto.Span.EndPosition);

        public IfNotGotoStatementSyntax(SyntaxToken ifToken, UnaryExpressionSyntax comparison, GotoStatementSyntax gotoStatement)
        {
            ifToken.Parent = this;
            comparison.Parent = this;
            gotoStatement.Parent = this;

            If = ifToken;
            Comparison = comparison;
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
            position = Comparison.UpdatePosition(position, ref line, ref column);
            position = Goto.UpdatePosition(position, ref line, ref column);

            If = ifToken;

            return position;
        }
    }
}
