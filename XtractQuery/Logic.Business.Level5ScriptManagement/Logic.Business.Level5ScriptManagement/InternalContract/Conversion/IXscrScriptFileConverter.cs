using Logic.Domain.CodeAnalysis.Contract.Level5.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IXscrScriptFileConverter
{
    CodeUnitSyntax CreateCodeUnit(XscrScriptFile script);
}
