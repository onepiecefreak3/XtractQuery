using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract.Exceptions
{
    public class JamCrcChecksumException : Exception
    {
        public JamCrcChecksumException()
        {
        }

        public JamCrcChecksumException(string message) : base(message)
        {
        }

        public JamCrcChecksumException(string message, Exception inner) : base(message, inner)
        {
        }

        protected JamCrcChecksumException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
