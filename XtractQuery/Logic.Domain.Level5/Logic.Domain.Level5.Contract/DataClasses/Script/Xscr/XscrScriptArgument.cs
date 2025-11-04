namespace Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;

public class XscrScriptArgument
{
    public int RawArgumentType { get; set; }
    public ScriptArgumentType Type { get; set; }
    public object Value { get; set; }
}