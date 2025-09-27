using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

public class XscrCompressionContainer
{
    public CompressedScriptTable InstructionTable { get; set; }
    public CompressedScriptTable ArgumentTable { get; set; }
    public CompressedScriptStringTable StringTable { get; set; }
}
