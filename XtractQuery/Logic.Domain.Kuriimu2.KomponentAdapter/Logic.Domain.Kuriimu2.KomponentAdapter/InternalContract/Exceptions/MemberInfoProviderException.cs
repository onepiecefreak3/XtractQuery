using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Kuriimu2.KomponentAdapter.InternalContract.Exceptions
{
    internal class MemberInfoProviderException:Exception
    {
        public MemberInfoProviderException()
        {
        }

        public MemberInfoProviderException(string message) : base(message)
        {
        }

        public MemberInfoProviderException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MemberInfoProviderException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
