using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Business.Level5ScriptManagement.InternalContract.Exceptions;
using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Business.Level5ScriptManagement.InternalContract
{
    [MapException(typeof(Level5SyntaxTreeConverterException))]
    public interface ILevel5CodeUnitConverter
    {
        ScriptFile CreateScriptFile(CodeUnitSyntax tree, ScriptType type);
    }
}
