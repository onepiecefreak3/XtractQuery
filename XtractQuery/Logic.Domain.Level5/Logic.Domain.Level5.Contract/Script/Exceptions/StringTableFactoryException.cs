using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script.Exceptions
{
    public class StringTableFactoryException : Exception
    {
        public StringTableFactoryException()
        {
        }

        public StringTableFactoryException(string message) : base(message)
        {
        }

        public StringTableFactoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected StringTableFactoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
