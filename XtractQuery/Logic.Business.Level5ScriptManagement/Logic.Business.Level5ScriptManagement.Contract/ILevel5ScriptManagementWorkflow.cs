using CrossCutting.Core.Contract.Aspects;
using Logic.Business.Level5ScriptManagement.Contract.Exceptions;

namespace Logic.Business.Level5ScriptManagement.Contract
{
    [MapException(typeof(Level5ScriptManagementWorkflowException))]
    public interface ILevel5ScriptManagementWorkflow
    {
        int Execute();
    }
}
