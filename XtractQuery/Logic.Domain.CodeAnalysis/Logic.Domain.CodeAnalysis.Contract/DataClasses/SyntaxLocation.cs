using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Contract.DataClasses
{
    public struct SyntaxLocation
    {
        public int Line { get; }
        public int Column { get; }

        public SyntaxLocation(int line, int column)
        {
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"({Line}, {Column})";
        }
    }
}
