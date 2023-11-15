using System;

namespace CrossCutting.Core.Contract.Aspects
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EncryptionAttribute : Attribute
    {
    }
}
