using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class ScriptTypeReaderException : Exception
    {
        public ScriptTypeReaderException()
        {
        }

        public ScriptTypeReaderException(string message) : base(message)
        {
        }

        public ScriptTypeReaderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptTypeReaderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
