using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class ScriptCompressorFactoryException : Exception
    {
        public ScriptCompressorFactoryException()
        {
        }

        public ScriptCompressorFactoryException(string message) : base(message)
        {
        }

        public ScriptCompressorFactoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptCompressorFactoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
