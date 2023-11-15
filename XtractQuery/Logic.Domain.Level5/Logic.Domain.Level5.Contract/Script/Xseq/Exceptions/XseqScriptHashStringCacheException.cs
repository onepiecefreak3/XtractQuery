using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Xseq.Exceptions
{
    public class XseqScriptHashStringCacheException:Exception
    {
        public XseqScriptHashStringCacheException()
        {
        }

        public XseqScriptHashStringCacheException(string message) : base(message)
        {
        }

        public XseqScriptHashStringCacheException(string message, Exception inner) : base(message, inner)
        {
        }

        protected XseqScriptHashStringCacheException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
