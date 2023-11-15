using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Script.Xq32.InternalContract.Exceptions;

namespace Logic.Domain.Level5.Script.Xq32.InternalContract
{
    [MapException(typeof(Xq32ScriptDecompressorException))]
    public interface IXq32ScriptDecompressor : IScriptDecompressor
    {
    }
}
