using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.Exceptions
{
    public class Level5ScriptParserException:Exception
    {
        public Level5ScriptParserException()
        {
        }

        public Level5ScriptParserException(string message) : base(message)
        {
        }

        public Level5ScriptParserException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Level5ScriptParserException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
