using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Contract.Exceptions
{
    public class ChecksumException : Exception
    {
        public ChecksumException()
        {
        }

        public ChecksumException(string message) : base(message)
        {
        }

        public ChecksumException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ChecksumException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
