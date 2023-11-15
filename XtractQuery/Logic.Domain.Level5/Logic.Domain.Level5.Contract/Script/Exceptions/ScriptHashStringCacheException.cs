using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    [Serializable]
    public class ScriptHashStringCacheException : Exception
    {
        public ScriptHashStringCacheException()
        {
        }

        public ScriptHashStringCacheException(string message) : base(message)
        {
        }

        public ScriptHashStringCacheException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ScriptHashStringCacheException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
