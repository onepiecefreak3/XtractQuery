﻿using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xseq.Exceptions;

namespace Logic.Domain.Level5.Contract.Script.Xseq
{
    [MapException(typeof(XseqScriptReaderException))]
    public interface IXseqScriptReader : IScriptReader
    {
        IReadOnlyList<XseqFunction> ReadFunctions(Stream functionStream, int entryCount);
        IReadOnlyList<XseqJump> ReadJumps(Stream jumpStream, int entryCount);
        IReadOnlyList<XseqInstruction> ReadInstructions(Stream instructionStream, int entryCount);
        IReadOnlyList<XseqArgument> ReadArguments(Stream argumentStream, int entryCount);

        IList<ScriptFunction> CreateFunctions(IReadOnlyList<XseqFunction> functions, ScriptStringTable? stringTable = null);
        IList<ScriptJump> ReadJumps(IReadOnlyList<XseqJump> jumps, ScriptStringTable? stringTable = null);
        IList<ScriptInstruction> ReadInstructions(IReadOnlyList<XseqInstruction> instructions);
        IList<ScriptArgument> ReadArguments(IReadOnlyList<XseqArgument> arguments, ScriptStringTable? stringTable = null);
    }
}
