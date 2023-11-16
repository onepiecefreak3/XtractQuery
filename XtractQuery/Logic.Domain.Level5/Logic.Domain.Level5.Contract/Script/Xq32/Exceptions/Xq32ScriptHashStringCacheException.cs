using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Xq32.Exceptions
{
    public class Xq32ScriptHashStringCacheException:Exception
    {
        public Xq32ScriptHashStringCacheException()
        {
        }

        public Xq32ScriptHashStringCacheException(string message) : base(message)
        {
        }

        public Xq32ScriptHashStringCacheException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Xq32ScriptHashStringCacheException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
