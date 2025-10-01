namespace Logic.Domain.Level5.Contract.Script.Gds.DataClasses;

public class GdsScriptInstruction
{
    public int Type { get; set; }
    public GdsScriptArgument[] Arguments { get; set; }
    public GdsScriptJump? Jump { get; set; }
}