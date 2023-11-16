using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses
{
    public readonly struct SyntaxTokenTrivia
    {
        private readonly int _position;
        private readonly int _line;
        private readonly int _column;

        public string Text { get; }

        public SyntaxLocation Location => new(_line, _column);
        public SyntaxSpan Span => new(_position, _position + Text.Length);

        internal SyntaxTokenTrivia(string text, int position, int line, int column) : this(text)
        {
            _position = position;
            _line = line;
            _column = column;
        }

        public SyntaxTokenTrivia(string text)
        {
            Text = text;
        }
    }
}
