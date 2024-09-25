using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq.Exceptions;

namespace Logic.Domain.Level5.Contract.Script.Xseq
{
    [MapException(typeof(XseqScriptReaderException))]
    public interface IXseqScriptReader : IScriptReader
    {
        IReadOnlyList<XseqFunction> ReadFunctions(Stream functionStream, int entryCount, PointerLength length);
        IReadOnlyList<XseqJump> ReadJumps(Stream jumpStream, int entryCount, PointerLength length);
        IReadOnlyList<XseqInstruction> ReadInstructions(Stream instructionStream, int entryCount, PointerLength length);
        IReadOnlyList<XseqArgument> ReadArguments(Stream argumentStream, int entryCount, PointerLength length);

        IList<ScriptFunction> CreateFunctions(IReadOnlyList<XseqFunction> functions, ScriptStringTable? stringTable = null);
        IList<ScriptJump> CreateJumps(IReadOnlyList<XseqJump> jumps, ScriptStringTable? stringTable = null);
        IList<ScriptInstruction> CreateInstructions(IReadOnlyList<XseqInstruction> instructions);
        IList<ScriptArgument> CreateArguments(IReadOnlyList<XseqArgument> arguments, IReadOnlyList<ScriptInstruction> instructions, ScriptStringTable? stringTable = null);
    }
}
