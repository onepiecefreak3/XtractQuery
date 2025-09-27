using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xscr;

public interface IXscrScriptParser
{
    XscrScriptFile Parse(Stream input);
    XscrScriptFile Parse(XscrCompressionContainer container);
    XscrScriptFile Parse(XscrScriptContainer container);

    IList<XscrScriptInstruction> ParseInstructions(XscrInstruction[] instructions);
    IList<XscrScriptArgument> ParseArguments(XscrArgument[] arguments, ScriptStringTable? stringTable = null);
}