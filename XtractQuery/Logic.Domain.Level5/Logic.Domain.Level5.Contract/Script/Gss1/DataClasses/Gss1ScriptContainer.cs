using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gss1.DataClasses;

public class Gss1ScriptContainer
{
    public required Gss1Function[] Functions { get; set; }
    public required Gss1Jump[] Jumps { get; set; }
    public required Gss1Instruction[] Instructions { get; set; }
    public required Gss1Argument[] Arguments { get; set; }
    public required ScriptStringTable Strings { get; set; }

    public int GlobalVariableCount { get; set; }
}
