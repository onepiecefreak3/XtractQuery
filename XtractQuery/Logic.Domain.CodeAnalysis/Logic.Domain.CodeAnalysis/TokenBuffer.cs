using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract;

namespace Logic.Domain.CodeAnalysis
{
    internal class TokenBuffer<TToken> : Buffer<TToken>
        where TToken : struct
    {
        private readonly ILexer<TToken> _lexer;

        public override bool IsEndOfInput { get; protected set; }

        public TokenBuffer(ILexer<TToken> lexer)
        {
            _lexer = lexer;
        }

        protected override TToken ReadInternal()
        {
            TToken value = _lexer.Read();
            IsEndOfInput = _lexer.IsEndOfInput;

            return value;
        }
    }
}
