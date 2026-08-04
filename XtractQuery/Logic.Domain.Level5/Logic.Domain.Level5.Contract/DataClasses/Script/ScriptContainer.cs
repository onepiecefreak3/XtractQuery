namespace Logic.Domain.Level5.Contract.DataClasses.Script;

public class ScriptContainer
{
    public required CompressedScriptTable FunctionTable { get; set; }
    public required CompressedScriptTable JumpTable { get; set; }
    public required CompressedScriptTable InstructionTable { get; set; }
    public required CompressedScriptTable ArgumentTable { get; set; }
    public required CompressedScriptStringTable StringTable { get; set; }

    public int GlobalVariableCount { get; set; }
}