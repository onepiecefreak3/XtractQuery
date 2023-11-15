using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.CodeAnalysis.Contract.Level5.Exceptions
{
    [Serializable]
    public class Level5SyntaxFactoryException : Exception
    {
        public Level5SyntaxFactoryException()
        {
        }

        public Level5SyntaxFactoryException(string message) : base(message)
        {
        }

        public Level5SyntaxFactoryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Level5SyntaxFactoryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
