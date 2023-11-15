using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract.Exceptions
{
    internal class Level5RleCompressionException:Exception
    {
        public Level5RleCompressionException()
        {
        }

        public Level5RleCompressionException(string message) : base(message)
        {
        }

        public Level5RleCompressionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Level5RleCompressionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
