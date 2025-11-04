namespace Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;

public class XscrScriptContainer
{
    public required XscrInstruction[] Instructions { get; set; }
    public required XscrArgument[] Arguments { get; set; }
    public required CompressedScriptStringTable Strings { get; set; }
}