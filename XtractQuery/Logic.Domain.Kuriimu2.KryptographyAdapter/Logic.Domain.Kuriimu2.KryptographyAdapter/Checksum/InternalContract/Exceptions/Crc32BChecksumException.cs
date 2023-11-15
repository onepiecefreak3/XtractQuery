using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract.Exceptions
{
    public class Crc32BChecksumException : Exception
    {
        public Crc32BChecksumException()
        {
        }

        public Crc32BChecksumException(string message) : base(message)
        {
        }

        public Crc32BChecksumException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Crc32BChecksumException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
