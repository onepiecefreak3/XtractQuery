using Logic.Domain.Level5.Contract.DataClasses.Script;
using System.Diagnostics.CodeAnalysis;

namespace Logic.Business.Level5ScriptManagement.InternalContract;

interface IScriptTypeConverter
{
    bool TryConvert(string? type, [NotNullWhen(true)] out ScriptType? scriptType);
}