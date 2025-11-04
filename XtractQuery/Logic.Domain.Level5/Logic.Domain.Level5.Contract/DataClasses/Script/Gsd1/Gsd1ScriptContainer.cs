namespace Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;

public class Gsd1ScriptContainer
{
    public required Gsd1Instruction[] Instructions { get; set; }
    public required Gsd1Argument[] Arguments { get; set; }
    public required ScriptStringTable Strings { get; set; }
}
