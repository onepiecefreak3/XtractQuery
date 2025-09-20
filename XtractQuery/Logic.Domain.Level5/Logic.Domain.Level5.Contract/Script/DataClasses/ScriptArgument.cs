namespace Logic.Domain.Level5.Contract.Script.DataClasses;

public class ScriptArgument
{
    public int RawArgumentType { get; set; }
    public ScriptArgumentType Type { get; set; }
    public object Value { get; set; }
}