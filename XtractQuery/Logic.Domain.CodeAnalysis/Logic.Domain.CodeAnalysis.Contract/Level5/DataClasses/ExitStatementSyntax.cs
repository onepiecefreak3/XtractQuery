using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract.DataClasses;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses
{
    public class ExitStatementSyntax : StatementSyntax
    {
        public SyntaxToken Exit { get; private set; }
        public SyntaxToken Semicolon { get; private set; }

        public override SyntaxLocation Location => Exit.FullLocation;
        public override SyntaxSpan Span => new(Exit.FullSpan.Position, Semicolon.FullSpan.EndPosition);

        public ExitStatementSyntax(SyntaxToken exit, SyntaxToken semicolon)
        {
            exit.Parent = this;
            semicolon.Parent = this;

            Exit = exit;
            Semicolon = semicolon;

            Root.Update();
        }

        public void SetExit(SyntaxToken yield, bool updatePositions = true)
        {
            yield.Parent = this;

            Exit = yield;

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
            SyntaxToken exit = Exit;
            SyntaxToken semicolon = Semicolon;

            position = exit.UpdatePosition(position, ref line, ref column);
            position = semicolon.UpdatePosition(position, ref line, ref column);

            Exit = exit;
            Semicolon = semicolon;

            return position;
        }
    }
}
