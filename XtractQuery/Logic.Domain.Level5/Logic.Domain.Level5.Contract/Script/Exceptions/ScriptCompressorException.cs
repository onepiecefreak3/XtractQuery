using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class ScriptCompressorException : Exception
    {
        public ScriptCompressorException()
        {
        }

        public ScriptCompressorException(string message) : base(message)
        {
        }

        public ScriptCompressorException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptCompressorException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
