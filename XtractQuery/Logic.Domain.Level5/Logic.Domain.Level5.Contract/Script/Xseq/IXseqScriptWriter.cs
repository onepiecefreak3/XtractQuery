﻿using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq.Exceptions;

namespace Logic.Domain.Level5.Contract.Script.Xseq
{
    [MapException(typeof(XseqScriptWriterException))]
    public interface IXseqScriptWriter : IScriptWriter
    {
        void WriteFunctions(IReadOnlyList<XseqFunction> functions, Stream output, PointerLength length);
        void WriteJumps(IReadOnlyList<XseqJump> jumps, Stream output, PointerLength length);
        void WriteInstructions(IReadOnlyList<XseqInstruction> instructions, Stream output, PointerLength length);
        void WriteArguments(IReadOnlyList<XseqArgument> arguments, Stream output, PointerLength length);
    }
}
