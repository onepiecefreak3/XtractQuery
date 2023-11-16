using Logic.Domain.Level5.Contract.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5
{
    internal abstract class ScriptHashStringCache<THash> : IScriptHashStringCache<THash>
        where THash : notnull
    {
        private readonly IDictionary<THash, string> _lookup;

        public ScriptHashStringCache()
        {
            _lookup = new Dictionary<THash, string>();
        }

        public string Get(THash hash)
        {
            return _lookup[hash];
        }

        public bool TryGet(THash hash, out string? value)
        {
            return _lookup.TryGetValue(hash, out value);
        }

        public void Set(THash hash, string value)
        {
            _lookup[hash] = value;
        }
    }
}
