using Logic.Domain.CodeAnalysis.Contract.DataClasses;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses
{
    public class LogicalExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Left { get; private set; }
        public SyntaxToken Operation { get; private set; }
        public ExpressionSyntax Right { get; private set; }

        public override SyntaxLocation Location => Left.Location;
        public override SyntaxSpan Span => new(Left.Span.Position, Right.Span.EndPosition);

        public LogicalExpressionSyntax(ExpressionSyntax left, SyntaxToken operation, ExpressionSyntax right)
        {
            left.Parent = this;
            operation.Parent = this;
            right.Parent = this;

            Left = left;
            Operation = operation;
            Right = right;

            Root.Update();
        }

        public void SetLeftExpression(ExpressionSyntax left, bool updatePositions = true)
        {
            left.Parent = this;

            Left = left;

            if (updatePositions)
                Root.Update();
        }

        public void SetOperation(SyntaxToken operation, bool updatePositions = true)
        {
            operation.Parent = this;

            Operation = operation;

            if (updatePositions)
                Root.Update();
        }

        public void SetRightExpression(ExpressionSyntax right, bool updatePositions = true)
        {
            right.Parent = this;

            Right = right;

            if (updatePositions)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken operation = Operation;

            position = Left.UpdatePosition(position, ref line, ref column);
            position = operation.UpdatePosition(position, ref line, ref column);
            position = Right.UpdatePosition(position, ref line, ref column);

            Operation = operation;

            return position;
        }
    }
}
