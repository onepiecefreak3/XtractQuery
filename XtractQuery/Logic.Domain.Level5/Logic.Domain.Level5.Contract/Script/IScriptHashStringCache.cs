using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Level5.Contract.Script.Exceptions;

namespace Logic.Domain.Level5.Contract.Script
{
    [MapException(typeof(ScriptHashStringCacheException))]
    public interface IScriptHashStringCache<in THash>
    {
        string Get(THash hash);
        bool TryGet(THash hash, out string? value);

        void Set(THash hash, string value);
    }
}
