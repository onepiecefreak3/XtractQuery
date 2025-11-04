using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gss1;

namespace Logic.Domain.Level5.Contract.Script.Gss1;

public interface IGss1ScriptParser
{
    Gss1ScriptFile Parse(Stream input);
    Gss1ScriptFile Parse(Gss1ScriptContainer container);

    IList<ScriptFunction> ParseFunctions(Gss1Function[] functions, ScriptStringTable? stringTable = null);
    IList<ScriptJump> ParseJumps(Gss1Jump[] jumps, ScriptStringTable? stringTable = null);
    IList<ScriptInstruction> ParseInstructions(Gss1Instruction[] instructions);
    IList<ScriptArgument> ParseArguments(Gss1Argument[] arguments, IReadOnlyList<ScriptInstruction> instructions, ScriptStringTable? stringTable = null);
}