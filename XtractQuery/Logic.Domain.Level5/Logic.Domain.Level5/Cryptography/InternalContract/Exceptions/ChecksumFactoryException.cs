using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Cryptography.InternalContract.Exceptions
{
    public class ChecksumFactoryException : Exception
    {
        public ChecksumFactoryException()
        {
        }

        public ChecksumFactoryException(string message) : base(message)
        {
        }

        public ChecksumFactoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ChecksumFactoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
