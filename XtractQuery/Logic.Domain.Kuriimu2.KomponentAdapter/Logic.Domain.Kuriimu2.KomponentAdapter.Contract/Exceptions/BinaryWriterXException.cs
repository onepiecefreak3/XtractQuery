using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions
{
    public class BinaryWriterXException:Exception
    {
        public BinaryWriterXException()
        {
        }

        public BinaryWriterXException(string message) : base(message)
        {
        }

        public BinaryWriterXException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BinaryWriterXException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
