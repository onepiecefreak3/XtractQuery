using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Contract.Exceptions
{
    [Serializable]
    public class BufferException : Exception
    {
        public BufferException()
        {
        }

        public BufferException(string message) : base(message)
        {
        }

        public BufferException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BufferException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
