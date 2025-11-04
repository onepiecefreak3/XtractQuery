using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xq32;

namespace Logic.Domain.Level5.Contract.Script.Xq32;

public interface IXq32ScriptReader : ICompressedScriptReader
{
    IReadOnlyList<Xq32Function> ReadFunctions(Stream functionStream, int entryCount, PointerLength length);
    IReadOnlyList<Xq32Jump> ReadJumps(Stream jumpStream, int entryCount, PointerLength length);
    IReadOnlyList<Xq32Instruction> ReadInstructions(Stream instructionStream, int entryCount, PointerLength length);
    IReadOnlyList<Xq32Argument> ReadArguments(Stream argumentStream, int entryCount, PointerLength length);

    IList<ScriptFunction> CreateFunctions(IReadOnlyList<Xq32Function> functions, CompressedScriptStringTable? stringTable = null);
    IList<ScriptJump> CreateJumps(IReadOnlyList<Xq32Jump> jumps, CompressedScriptStringTable? stringTable = null);
    IList<ScriptInstruction> CreateInstructions(IReadOnlyList<Xq32Instruction> instructions);
    IList<ScriptArgument> CreateArguments(IReadOnlyList<Xq32Argument> arguments, IReadOnlyList<ScriptInstruction> instructions, CompressedScriptStringTable? stringTable = null);
}