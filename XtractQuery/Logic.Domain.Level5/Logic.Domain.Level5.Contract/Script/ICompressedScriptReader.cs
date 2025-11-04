using Logic.Domain.Level5.Contract.DataClasses.Script;

namespace Logic.Domain.Level5.Contract.Script;

public interface ICompressedScriptReader
{
    ScriptFile Read(ScriptContainer container);
    ScriptFile Read(Stream input);

    IList<ScriptFunction> ReadFunctions(CompressedScriptTable functionTable, CompressedScriptStringTable? stringTable);
    IList<ScriptJump> ReadJumps(CompressedScriptTable jumpTable, CompressedScriptStringTable? stringTable);
    IList<ScriptInstruction> ReadInstructions(CompressedScriptTable instructionTable);
    IList<ScriptArgument> ReadArguments(CompressedScriptTable argumentTable, CompressedScriptTable instructionTable, CompressedScriptStringTable? stringTable);
}