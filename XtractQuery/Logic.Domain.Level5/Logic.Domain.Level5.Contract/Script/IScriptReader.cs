using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script;

public interface IScriptReader
{
    ScriptFile Read(ScriptContainer container);
    ScriptFile Read(Stream input);

    IList<ScriptFunction> ReadFunctions(ScriptTable functionTable, ScriptStringTable? stringTable);
    IList<ScriptJump> ReadJumps(ScriptTable jumpTable, ScriptStringTable? stringTable);
    IList<ScriptInstruction> ReadInstructions(ScriptTable instructionTable);
    IList<ScriptArgument> ReadArguments(ScriptTable argumentTable, ScriptTable instructionTable, ScriptStringTable? stringTable);
}