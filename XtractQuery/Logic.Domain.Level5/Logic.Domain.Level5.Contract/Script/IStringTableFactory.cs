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
    [MapException(typeof(StringTableFactoryException))]
    public interface IStringTableFactory
    {
        IStringTable Create(Stream input, ScriptType type);
        IStringTable Create(ScriptType scriptType);
    }
}
