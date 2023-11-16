using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class ScriptWriterException : Exception
    {
        public ScriptWriterException()
        {
        }

        public ScriptWriterException(string message) : base(message)
        {
        }

        public ScriptWriterException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptWriterException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
