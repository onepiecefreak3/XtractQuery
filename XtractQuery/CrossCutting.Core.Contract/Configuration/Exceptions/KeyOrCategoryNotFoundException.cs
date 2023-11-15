using System;
using System.Runtime.Serialization;

namespace CrossCutting.Core.Contract.Configuration.Exceptions
{
    [Serializable]
    public class KeyOrCategoryNotFoundException : ConfigurationException
    {
        public KeyOrCategoryNotFoundException()
        {
        }

        public KeyOrCategoryNotFoundException(string category, string key)
            : this($"No config entry found for category: {category} and/or key: {key}")
        {

        }

        public KeyOrCategoryNotFoundException(string message) : base(message)
        {
        }

        public KeyOrCategoryNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        private KeyOrCategoryNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
