using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Exceptions
{
    [Serializable]
    public class Level5ScriptFileConverterException : Exception
    {
        public Level5ScriptFileConverterException()
        {
        }

        public Level5ScriptFileConverterException(string message) : base(message)
        {
        }

        public Level5ScriptFileConverterException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Level5ScriptFileConverterException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
