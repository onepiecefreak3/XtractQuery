using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract.Exceptions
{
    public class Crc16ModBusChecksumException : Exception
    {
        public Crc16ModBusChecksumException()
        {
        }

        public Crc16ModBusChecksumException(string message) : base(message)
        {
        }

        public Crc16ModBusChecksumException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Crc16ModBusChecksumException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
