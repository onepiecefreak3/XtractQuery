using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Exceptions
{
    [Serializable]
    public class MethodNameMapperException : Exception
    {
        public MethodNameMapperException()
        {
        }

        public MethodNameMapperException(string message) : base(message)
        {
        }

        public MethodNameMapperException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MethodNameMapperException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
