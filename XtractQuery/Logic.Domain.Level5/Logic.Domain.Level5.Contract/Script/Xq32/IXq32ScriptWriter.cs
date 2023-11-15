using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.Xq32.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xq32.Exceptions;

namespace Logic.Domain.Level5.Contract.Script.Xq32
{
    [MapException(typeof(Xq32ScriptWriterException))]
    public interface IXq32ScriptWriter : IScriptWriter
    {
        void WriteFunctions(IReadOnlyList<Xq32Function> functions, Stream output);
        void WriteJumps(IReadOnlyList<Xq32Jump> jumps, Stream output);
        void WriteInstructions(IReadOnlyList<Xq32Instruction> instructions, Stream output);
        void WriteArguments(IReadOnlyList<Xq32Argument> arguments, Stream output);
    }
}
