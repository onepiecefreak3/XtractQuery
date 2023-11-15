using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Exceptions;

namespace Logic.Domain.Level5.Contract.Script
{
    [MapException(typeof(ScriptDecompressorException))]
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
}
