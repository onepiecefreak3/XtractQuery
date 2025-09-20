namespace Logic.Business.Level5ScriptManagement.InternalContract;

public interface IMethodNameMapper
{
    bool MapsInstructionType(int instructionType);
    bool MapsMethodName(string methodName);

    string GetMethodName(int instructionType);
    int GetInstructionType(string methodName);
}