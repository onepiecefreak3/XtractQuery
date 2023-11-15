using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions
{
    internal class BinaryTypeReaderException : Exception
    {
        public BinaryTypeReaderException()
        {
        }

        public BinaryTypeReaderException(string message) : base(message)
        {
        }

        public BinaryTypeReaderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BinaryTypeReaderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
