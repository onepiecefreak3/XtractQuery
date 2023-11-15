using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses
{
    public class VariableExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken Variable { get; private set; }

        public override SyntaxLocation Location => Variable.FullLocation;
        public override SyntaxSpan Span => Variable.FullSpan;

        public VariableExpressionSyntax(SyntaxToken variable)
        {
            variable.Parent = this;

            Variable = variable;

            Root.Update();
        }

        public void SetVariable(SyntaxToken variable, bool updatePositions = true)
        {
            variable.Parent = this;

            Variable = variable;

            if (updatePositions)
                Root.Update();
        }

        internal override int UpdatePosition(int position, ref int line, ref int column)
        {
            SyntaxToken variable = Variable;

            position = variable.UpdatePosition(position, ref line, ref column);

            Variable = variable;

            return position;
        }
    }
}
