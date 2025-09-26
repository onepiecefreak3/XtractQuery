using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gsd1.DataClasses;

public class Gsd1ScriptArgument
{
    public int RawArgumentType { get; set; }
    public ScriptArgumentType Type { get; set; }
    public object Value { get; set; }
}