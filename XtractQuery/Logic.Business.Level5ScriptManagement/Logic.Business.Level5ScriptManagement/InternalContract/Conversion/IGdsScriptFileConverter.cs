using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script.Gds;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

interface IGdsScriptFileConverter
{
    CodeUnitSyntax CreateCodeUnit(GdsScriptFile script);
}