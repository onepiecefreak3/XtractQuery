using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Script.Xseq.InternalContract.Exceptions
{
    public class XseqStringTableException : Exception
    {
        public XseqStringTableException()
        {
        }

        public XseqStringTableException(string message) : base(message)
        {
        }

        public XseqStringTableException(string message, Exception inner) : base(message, inner)
        {
        }

        protected XseqStringTableException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
