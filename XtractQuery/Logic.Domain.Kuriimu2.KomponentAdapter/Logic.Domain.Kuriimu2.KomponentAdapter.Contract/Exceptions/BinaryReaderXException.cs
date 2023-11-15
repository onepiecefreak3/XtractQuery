using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions
{
    public class BinaryReaderXException:Exception
    {
        public BinaryReaderXException()
        {
        }

        public BinaryReaderXException(string message) : base(message)
        {
        }

        public BinaryReaderXException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BinaryReaderXException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
