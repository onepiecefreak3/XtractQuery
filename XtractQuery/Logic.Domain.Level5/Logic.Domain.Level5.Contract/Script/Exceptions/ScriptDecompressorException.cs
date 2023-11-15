using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class ScriptDecompressorException : Exception
    {
        public ScriptDecompressorException()
        {
        }

        public ScriptDecompressorException(string message) : base(message)
        {
        }

        public ScriptDecompressorException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptDecompressorException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
