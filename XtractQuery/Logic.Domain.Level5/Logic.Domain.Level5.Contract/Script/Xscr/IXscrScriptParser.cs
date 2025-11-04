using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;

namespace Logic.Domain.Level5.Contract.Script.Xscr;

public interface IXscrScriptParser
{
    XscrScriptFile Parse(Stream input);
    XscrScriptFile Parse(XscrCompressionContainer container);
    XscrScriptFile Parse(XscrScriptContainer container);

    IList<XscrScriptInstruction> ParseInstructions(XscrInstruction[] instructions);
    IList<XscrScriptArgument> ParseArguments(XscrArgument[] arguments, ScriptStringTable? stringTable = null);
}