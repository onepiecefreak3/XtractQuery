namespace Logic.Domain.Level5.Contract.Script.DataClasses;

public class ScriptContainer
{
    public CompressedScriptTable FunctionTable { get; set; }
    public CompressedScriptTable JumpTable { get; set; }
    public CompressedScriptTable InstructionTable { get; set; }
    public CompressedScriptTable ArgumentTable { get; set; }
    public CompressedScriptStringTable StringTable { get; set; }

    public int GlobalVariableCount { get; set; }
}