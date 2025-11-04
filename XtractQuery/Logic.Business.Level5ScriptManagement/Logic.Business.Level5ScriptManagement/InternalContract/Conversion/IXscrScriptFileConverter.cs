using Logic.Domain.CodeAnalysis.Contract.DataClasses.Level5;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;

namespace Logic.Business.Level5ScriptManagement.InternalContract.Conversion;

public interface IXscrScriptFileConverter
{
    CodeUnitSyntax CreateCodeUnit(XscrScriptFile script);
}
