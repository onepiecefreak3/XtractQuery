using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.Xq32.Exceptions;

namespace Logic.Domain.Level5.Contract.Script.Xq32
{
    [MapException(typeof(Xq32ScriptHashStringCacheException))]
    public interface IXq32ScriptHashStringCache : IScriptHashStringCache<uint>
    {
    }
}
