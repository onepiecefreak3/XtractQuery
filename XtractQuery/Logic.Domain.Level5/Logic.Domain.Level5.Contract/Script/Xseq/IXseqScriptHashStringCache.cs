using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.Xseq.Exceptions;

namespace Logic.Domain.Level5.Contract.Script.Xseq
{
    [MapException(typeof(XseqScriptHashStringCacheException))]
    public interface IXseqScriptHashStringCache: IScriptHashStringCache<ushort>
    {
    }
}
