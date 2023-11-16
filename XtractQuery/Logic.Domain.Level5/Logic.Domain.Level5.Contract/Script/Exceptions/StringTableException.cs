using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class StringTableException : Exception
    {
        public StringTableException()
        {
        }

        public StringTableException(string message) : base(message)
        {
        }

        public StringTableException(string message, Exception inner) : base(message, inner)
        {
        }

        protected StringTableException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
