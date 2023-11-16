using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.Contract.Exceptions
{
    public class BinaryFactoryException : Exception
    {
        public BinaryFactoryException()
        {
        }

        public BinaryFactoryException(string message) : base(message)
        {
        }

        public BinaryFactoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BinaryFactoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
