using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Exceptions
{
    [Serializable]
    public class Level5SyntaxTreeConverterException : Exception
    {
        public Level5SyntaxTreeConverterException()
        {
        }

        public Level5SyntaxTreeConverterException(string message) : base(message)
        {
        }

        public Level5SyntaxTreeConverterException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Level5SyntaxTreeConverterException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
