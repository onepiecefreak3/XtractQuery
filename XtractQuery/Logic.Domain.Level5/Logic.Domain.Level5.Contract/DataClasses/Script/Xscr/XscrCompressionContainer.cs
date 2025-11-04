namespace Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;

public class XscrCompressionContainer
{
    public CompressedScriptTable InstructionTable { get; set; }
    public CompressedScriptTable ArgumentTable { get; set; }
    public CompressedScriptStringTable StringTable { get; set; }
}
