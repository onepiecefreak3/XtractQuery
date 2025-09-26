using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gsd1.DataClasses;

public class Gsd1ScriptContainer
{
    public required Gsd1Instruction[] Instructions { get; set; }
    public required Gsd1Argument[] Arguments { get; set; }
    public required ScriptStringTable Strings { get; set; }
}
