using System;

namespace CrossCutting.Core.Contract.Aspects;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Assembly, Inherited = true)]
public class ExceptionMessageAttribute(string message) : Attribute
{
    public string Message { get; } = message;
}