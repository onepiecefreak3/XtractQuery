namespace Logic.Domain.Level5.Contract.DataClasses.Script.Gds;

public class GdsScriptInstruction
{
    public int Type { get; set; }
    public required GdsScriptArgument[] Arguments { get; set; }
    public GdsScriptJump? Jump { get; set; }
}