using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions
{
    internal class BinaryTypeWriterException : Exception
    {
        public BinaryTypeWriterException()
        {
        }

        public BinaryTypeWriterException(string message) : base(message)
        {
        }

        public BinaryTypeWriterException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BinaryTypeWriterException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
