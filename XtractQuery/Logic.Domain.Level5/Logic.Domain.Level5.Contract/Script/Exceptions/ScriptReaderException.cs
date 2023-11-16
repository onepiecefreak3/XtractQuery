using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class ScriptReaderException : Exception
    {
        public ScriptReaderException()
        {
        }

        public ScriptReaderException(string message) : base(message)
        {
        }

        public ScriptReaderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptReaderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
