using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses
{
    [DebuggerDisplay("[{FullSpan.Position}..{FullSpan.EndPosition}) {Text}")]
    public struct SyntaxToken
    {
        private int _textPosition;

        public SyntaxNode? Parent { get; internal set; }

        public int RawKind { get; }
        public string Text { get; }

        public SyntaxTokenTrivia? LeadingTrivia { get; private set; }
        public SyntaxTokenTrivia? TrailingTrivia { get; private set; }

        public SyntaxLocation Location { get; private set; }
        public SyntaxLocation FullLocation { get; private set; }

        public SyntaxSpan Span => new(_textPosition, _textPosition + Text.Length);
        public SyntaxSpan FullSpan => new(LeadingTrivia?.Span.Position ?? _textPosition, TrailingTrivia?.Span.EndPosition ?? _textPosition + Text.Length);

        public SyntaxToken(string text, int rawKind, SyntaxTokenTrivia? leadingTrivia = null, SyntaxTokenTrivia? trailingTrivia = null)
        {
            RawKind = rawKind;
            Text = text;

            LeadingTrivia = leadingTrivia;
            TrailingTrivia = trailingTrivia;
        }

        public SyntaxToken WithLeadingTrivia(string? trivia)
        {
            LeadingTrivia = trivia == null ? null : new SyntaxTokenTrivia(trivia);

            return this;
        }

        public SyntaxToken WithTrailingTrivia(string? trivia)
        {
            TrailingTrivia = trivia == null ? null : new SyntaxTokenTrivia(trivia);

            return this;
        }

        public SyntaxToken WithNoTrivia()
        {
            LeadingTrivia = null;
            TrailingTrivia = null;

            return this;
        }

        internal int UpdatePosition(int fullPosition, ref int line, ref int column)
        {
            FullLocation = new(line, column);

            if (LeadingTrivia.HasValue)
            {
                LeadingTrivia = new SyntaxTokenTrivia(LeadingTrivia.Value.Text, fullPosition, line, column);
                fullPosition += LeadingTrivia.Value.Text.Length;

                AdvanceLineColumn(LeadingTrivia.Value.Text, ref line, ref column);
            }

            Location = new(line, column);

            _textPosition = fullPosition;
            AdvanceLineColumn(Text, ref line, ref column);

            if (TrailingTrivia.HasValue)
            {
                TrailingTrivia = new SyntaxTokenTrivia(TrailingTrivia.Value.Text, fullPosition, line, column);
                fullPosition += TrailingTrivia.Value.Text.Length;

                AdvanceLineColumn(TrailingTrivia.Value.Text, ref line, ref column);
            }

            return fullPosition;
        }

        private void AdvanceLineColumn(string? text, ref int line, ref int column)
        {
            if (string.IsNullOrEmpty(text))
                return;

            foreach (char character in text)
            {
                switch (character)
                {
                    case '\n':
                        line++;
                        column = 1;
                        break;

                    case '\t':
                        column += 4;
                        break;

                    default:
                        column++;
                        break;
                }
            }
        }
    }
}
