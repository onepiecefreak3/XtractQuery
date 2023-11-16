using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Level5.InternalContract.DataClasses
{
    internal struct WhitespaceNormalizeContext
    {
        public int Indent { get; set; }

        public bool ShouldIndent { get; set; }
        public bool ShouldLineBreak { get; set; }
        public bool IsFirstElement { get; set; }
    }
}
