using System.Diagnostics.CodeAnalysis;
using Logic.Business.Level5ScriptManagement.InternalContract;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Business.Level5ScriptManagement;

class ScriptTypeConverter : IScriptTypeConverter
{
    public bool TryConvert(string? type, [NotNullWhen(true)] out ScriptType? scriptType)
    {
        scriptType = null;

        switch (type)
        {
            case "xq32":
                scriptType = ScriptType.Xq32;
                break;

            case "xseq":
                scriptType = ScriptType.Xseq;
                break;

            case "xscr":
                scriptType = ScriptType.Xscr;
                break;

            case "gss1":
                scriptType = ScriptType.Gss1;
                break;

            case "gsd1":
                scriptType = ScriptType.Gsd1;
                break;

            case "gds":
                scriptType = ScriptType.Gds;
                break;

            default:
                return false;
        }

        return true;
    }
}