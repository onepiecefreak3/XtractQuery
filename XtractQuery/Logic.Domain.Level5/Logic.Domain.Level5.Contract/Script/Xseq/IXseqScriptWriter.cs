using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq.Exceptions;

namespace Logic.Domain.Level5.Contract.Script.Xseq
{
    [MapException(typeof(XseqScriptWriterException))]
    public interface IXseqScriptWriter : IScriptWriter
    {
        void WriteFunctions(IReadOnlyList<XseqFunction> functions, Stream output);
        void WriteJumps(IReadOnlyList<XseqJump> jumps, Stream output);
        void WriteInstructions(IReadOnlyList<XseqInstruction> instructions, Stream output);
        void WriteArguments(IReadOnlyList<XseqArgument> arguments, Stream output);
    }
}
