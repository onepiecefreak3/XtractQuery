using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.Exceptions
{
    internal class ValueStorageFactoryException:Exception
    {
        public ValueStorageFactoryException()
        {
        }

        public ValueStorageFactoryException(string message) : base(message)
        {
        }

        public ValueStorageFactoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ValueStorageFactoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
