using System;

namespace CrossCutting.Core.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Assembly, Inherited = true)]
    public class MapExceptionAttribute : Attribute
    {
        public Type TargetException { get; }
        public string Message { get; }

        public MapExceptionAttribute(Type targetException, string message = null)
        {
            TargetException = targetException;
            Message = message;
        }
    }
}
