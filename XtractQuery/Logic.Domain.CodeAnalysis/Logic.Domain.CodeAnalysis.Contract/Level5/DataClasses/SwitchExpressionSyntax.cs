using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses
{
    public class SwitchExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Value { get; private set; }
        public SyntaxToken Switch { get; private set; }
        public SwitchBlockExpressionSyntax CaseBlock { get; private set; }

        public override SyntaxLocation Location => Value.Location;
        public override SyntaxSpan Span => new(Value.Span.Position, CaseBlock.Span.EndPosition);

        public SwitchExpressionSyntax(ExpressionSyntax value, SyntaxToken switchToken, SwitchBlockExpressionSyntax caseBlock)
        {
            value.Parent = this;
            switchToken.Parent = this;
            caseBlock.Parent = this;

            Value = value;
            Switch = switchToken;
            CaseBlock = caseBlock;

            Root.Update();
        }

        public void SetValue(ExpressionSyntax value, bool updatePositions = true)
        {
            value.Parent = this;

            Value = value;

            if (updatePositions)
                Root.Update();
        }

        public void SetSwitch(SyntaxToken switchToken, bool updatePositions = true)
        {
            switchToken.Parent = this;

            Switch = switchToken;

            if (updatePositions)
                Root.Update();
        }

        public void SetCaseBlock(SwitchBlockExpressionSyntax caseBlock, bool updatePositions = true)
        {
            caseBlock.Parent = this;

            CaseBlock = caseBlock;

            if (updatePositions)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken switchToken = Switch;

            position = Value.UpdatePosition(position, ref line, ref column);
            position = switchToken.UpdatePosition(position, ref line, ref column);
            position = CaseBlock.UpdatePosition(position, ref line, ref column);

            Switch = switchToken;

            return position;
        }
    }
}
