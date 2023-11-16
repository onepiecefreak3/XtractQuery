using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.CodeAnalysis.Contract;

namespace Logic.Domain.CodeAnalysis
{
    internal class StringBuffer : Buffer<int>
    {
        private readonly TextReader _reader;

        public override bool IsEndOfInput { get; protected set; }

        public StringBuffer(string text)
        {
            _reader = new StringReader(text);
        }

        protected override int ReadInternal()
        {
            int value = _reader.Read();
            IsEndOfInput = value < 0;

            return value;
        }
    }
}
