using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

public class XscrScriptArgument
{
    public int RawArgumentType { get; set; }
    public ScriptArgumentType Type { get; set; }
    public object Value { get; set; }
}