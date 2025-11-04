namespace Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;

public class XscrScriptFile
{
    public IList<XscrScriptInstruction> Instructions { get; set; }
    public IList<XscrScriptArgument> Arguments { get; set; }
}