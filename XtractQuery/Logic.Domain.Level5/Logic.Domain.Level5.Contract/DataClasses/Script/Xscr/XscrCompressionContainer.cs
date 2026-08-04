namespace Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;

public class XscrCompressionContainer
{
    public required CompressedScriptTable InstructionTable { get; set; }
    public required CompressedScriptTable ArgumentTable { get; set; }
    public required CompressedScriptStringTable StringTable { get; set; }
}
