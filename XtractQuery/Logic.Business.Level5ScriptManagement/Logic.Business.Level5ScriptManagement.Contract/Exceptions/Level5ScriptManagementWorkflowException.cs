using System.Runtime.Serialization;

namespace Logic.Business.Level5ScriptManagement.Contract.Exceptions;

[Serializable]
public class Level5ScriptManagementWorkflowException : Exception
{
    public Level5ScriptManagementWorkflowException()
    {
    }

    public Level5ScriptManagementWorkflowException(string message) : base(message)
    {
    }

    public Level5ScriptManagementWorkflowException(string message, Exception inner) : base(message, inner)
    {
    }

    protected Level5ScriptManagementWorkflowException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}