using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Script.Xseq.InternalContract.Exceptions;

namespace Logic.Domain.Level5.Script.Xseq.InternalContract
{
    [MapException(typeof(XseqScriptCompressorException))]
    public interface IXseqScriptCompressor : IScriptCompressor
    {
    }
}
