using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;

namespace Logic.Domain.Level5.Contract.Script.Gsd1;

public interface IGsd1ScriptParser
{
    Gsd1ScriptFile Parse(Stream input);
    Gsd1ScriptFile Parse(Gsd1ScriptContainer container);

    IList<Gsd1ScriptInstruction> ParseInstructions(Gsd1Instruction[] instructions);
    IList<Gsd1ScriptArgument> ParseArguments(Gsd1Argument[] arguments, ScriptStringTable? stringTable = null);
}