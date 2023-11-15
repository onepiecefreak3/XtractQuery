using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Script.Xq32.InternalContract.Exceptions
{
    public class Xq32StringTableException : Exception
    {
        public Xq32StringTableException()
        {
        }

        public Xq32StringTableException(string message) : base(message)
        {
        }

        public Xq32StringTableException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Xq32StringTableException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
