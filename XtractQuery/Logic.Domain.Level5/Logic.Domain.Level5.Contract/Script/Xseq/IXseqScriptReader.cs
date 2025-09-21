using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xseq;

public interface IXseqScriptReader : ICompressedScriptReader
{
    IReadOnlyList<XseqFunction> ReadFunctions(Stream functionStream, int entryCount, PointerLength length);
    IReadOnlyList<XseqJump> ReadJumps(Stream jumpStream, int entryCount, PointerLength length);
    IReadOnlyList<XseqInstruction> ReadInstructions(Stream instructionStream, int entryCount, PointerLength length);
    IReadOnlyList<XseqArgument> ReadArguments(Stream argumentStream, int entryCount, PointerLength length);

    IList<ScriptFunction> CreateFunctions(IReadOnlyList<XseqFunction> functions, CompressedScriptStringTable? stringTable = null);
    IList<ScriptJump> CreateJumps(IReadOnlyList<XseqJump> jumps, CompressedScriptStringTable? stringTable = null);
    IList<ScriptInstruction> CreateInstructions(IReadOnlyList<XseqInstruction> instructions);
    IList<ScriptArgument> CreateArguments(IReadOnlyList<XseqArgument> arguments, IReadOnlyList<ScriptInstruction> instructions, CompressedScriptStringTable? stringTable = null);
}