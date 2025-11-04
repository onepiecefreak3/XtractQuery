namespace Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;

public class Gsd1ScriptArgument
{
    public int RawArgumentType { get; set; }
    public ScriptArgumentType Type { get; set; }
    public object Value { get; set; }
}