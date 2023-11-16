using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class ScriptDecompressorFactoryException : Exception
    {
        public ScriptDecompressorFactoryException()
        {
        }

        public ScriptDecompressorFactoryException(string message) : base(message)
        {
        }

        public ScriptDecompressorFactoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptDecompressorFactoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
