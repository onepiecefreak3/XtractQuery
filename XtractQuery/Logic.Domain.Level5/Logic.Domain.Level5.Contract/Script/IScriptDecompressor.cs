using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script;

public interface IScriptDecompressor
{
    ScriptContainer Decompress(Stream input);

    int GetGlobalVariableCount(Stream input);

    CompressedScriptTable DecompressFunctions(Stream input);
    CompressedScriptTable DecompressJumps(Stream input);
    CompressedScriptTable DecompressInstructions(Stream input);
    CompressedScriptTable DecompressArguments(Stream input);
    CompressedScriptStringTable DecompressStrings(Stream input);
}