using System;

namespace CrossCutting.Core.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Assembly, Inherited = true)]
    public class ExceptionMessageAttribute : Attribute
    {
        public string Message { get; }

        public ExceptionMessageAttribute(string message)
        {
            Message = message;
        }
    }
}