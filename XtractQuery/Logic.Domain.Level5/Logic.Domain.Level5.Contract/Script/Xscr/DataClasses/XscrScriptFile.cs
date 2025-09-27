namespace Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

public class XscrScriptFile
{
    public IList<XscrScriptInstruction> Instructions { get; set; }
    public IList<XscrScriptArgument> Arguments { get; set; }
}