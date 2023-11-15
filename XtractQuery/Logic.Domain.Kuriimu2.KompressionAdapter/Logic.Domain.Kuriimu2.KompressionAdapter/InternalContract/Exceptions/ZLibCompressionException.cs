using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract.Exceptions
{
    internal class ZLibCompressionException:Exception
    {
        public ZLibCompressionException()
        {
        }

        public ZLibCompressionException(string message) : base(message)
        {
        }

        public ZLibCompressionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ZLibCompressionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
