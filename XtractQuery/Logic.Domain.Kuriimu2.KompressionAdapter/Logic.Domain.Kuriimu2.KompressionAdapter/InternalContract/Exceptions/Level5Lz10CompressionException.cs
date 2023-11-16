using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract.Exceptions
{
    internal class Level5Lz10CompressionException:Exception
    {
        public Level5Lz10CompressionException()
        {
        }

        public Level5Lz10CompressionException(string message) : base(message)
        {
        }

        public Level5Lz10CompressionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Level5Lz10CompressionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
