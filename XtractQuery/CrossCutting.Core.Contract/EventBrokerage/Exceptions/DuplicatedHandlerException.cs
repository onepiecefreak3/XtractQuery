using System;
using System.Runtime.Serialization;

namespace CrossCutting.Core.Contract.EventBrokerage.Exceptions
{
    [Serializable]
    public class DuplicatedHandlerException : EventBrokerageException
    {
        public DuplicatedHandlerException()
        {
        }

        public DuplicatedHandlerException(string message) : base(message)
        {
        }

        public DuplicatedHandlerException(string message, Exception inner) : base(message, inner)
        {
        }

        private DuplicatedHandlerException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}