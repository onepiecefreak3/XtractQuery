using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Exceptions
{
    [Serializable]
    public class Level5ScriptManagementConfigurationValidationException : Exception
    {
        public Level5ScriptManagementConfigurationValidationException()
        {
        }

        public Level5ScriptManagementConfigurationValidationException(string message) : base(message)
        {
        }

        public Level5ScriptManagementConfigurationValidationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected Level5ScriptManagementConfigurationValidationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
