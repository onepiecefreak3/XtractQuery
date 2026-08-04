namespace Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;

public class XscrScriptFile
{
    public required IList<XscrScriptInstruction> Instructions { get; set; }
    public required IList<XscrScriptArgument> Arguments { get; set; }
}