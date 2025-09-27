using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

public class XscrScriptContainer
{
    public required XscrInstruction[] Instructions { get; set; }
    public required XscrArgument[] Arguments { get; set; }
    public required CompressedScriptStringTable Strings { get; set; }
}