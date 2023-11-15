using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xq32.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xq32.Exceptions;

namespace Logic.Domain.Level5.Contract.Script.Xq32
{
    [MapException(typeof(Xq32ScriptReaderException))]
    public interface IXq32ScriptReader : IScriptReader
    {
        IReadOnlyList<Xq32Function> ReadFunctions(Stream functionStream, int entryCount);
        IReadOnlyList<Xq32Jump> ReadJumps(Stream jumpStream, int entryCount);
        IReadOnlyList<Xq32Instruction> ReadInstructions(Stream instructionStream, int entryCount);
        IReadOnlyList<Xq32Argument> ReadArguments(Stream argumentStream, int entryCount);

        IList<ScriptFunction> CreateFunctions(IReadOnlyList<Xq32Function> functions, ScriptStringTable? stringTable = null);
        IList<ScriptJump> CreateJumps(IReadOnlyList<Xq32Jump> jumps, ScriptStringTable? stringTable = null);
        IList<ScriptInstruction> CreateInstructions(IReadOnlyList<Xq32Instruction> instructions);
        IList<ScriptArgument> CreateArguments(IReadOnlyList<Xq32Argument> arguments, ScriptStringTable? stringTable = null);
    }
}
