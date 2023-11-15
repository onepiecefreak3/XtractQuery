using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class ScriptWriterFactoryException : Exception
    {
        public ScriptWriterFactoryException()
        {
        }

        public ScriptWriterFactoryException(string message) : base(message)
        {
        }

        public ScriptWriterFactoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptWriterFactoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
