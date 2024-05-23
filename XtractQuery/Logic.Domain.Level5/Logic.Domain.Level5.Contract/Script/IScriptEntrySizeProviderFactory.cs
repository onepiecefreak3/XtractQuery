using Logic.Domain.Level5.Contract.Script.DataClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Domain.Level5.Contract.Script
{
    public interface IScriptEntrySizeProviderFactory
    {
        IScriptEntrySizeProvider Create(ScriptType type);
    }
}
