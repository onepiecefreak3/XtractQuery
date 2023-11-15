using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class ScriptReaderFactoryException : Exception
    {
        public ScriptReaderFactoryException()
        {
        }

        public ScriptReaderFactoryException(string message) : base(message)
        {
        }

        public ScriptReaderFactoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptReaderFactoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
