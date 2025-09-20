using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script;

public interface IScriptDecompressor
{
    ScriptContainer Decompress(Stream input);

    int GetGlobalVariableCount(Stream input);

    ScriptTable DecompressFunctions(Stream input);
    ScriptTable DecompressJumps(Stream input);
    ScriptTable DecompressInstructions(Stream input);
    ScriptTable DecompressArguments(Stream input);
    ScriptStringTable DecompressStrings(Stream input);
}