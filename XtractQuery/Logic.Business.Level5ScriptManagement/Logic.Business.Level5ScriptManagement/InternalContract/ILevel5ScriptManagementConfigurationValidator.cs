using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Business.Level5ScriptManagement.InternalContract.Exceptions;

namespace Logic.Business.Level5ScriptManagement.InternalContract
{
    [MapException(typeof(Level5ScriptManagementConfigurationValidationException))]
    public interface ILevel5ScriptManagementConfigurationValidator
    {
        void Validate(Level5ScriptManagementConfiguration config);
    }
}
