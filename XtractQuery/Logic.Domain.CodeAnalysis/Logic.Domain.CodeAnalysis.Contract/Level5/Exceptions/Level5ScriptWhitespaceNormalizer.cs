using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.Exceptions
{
    public class Level5ScriptWhitespaceNormalizer : Exception
    {
        public Level5ScriptWhitespaceNormalizer()
        {
        }

        public Level5ScriptWhitespaceNormalizer(string message) : base(message)
        {
        }

        public Level5ScriptWhitespaceNormalizer(string message, Exception inner) : base(message, inner)
        {
        }

        protected Level5ScriptWhitespaceNormalizer(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
