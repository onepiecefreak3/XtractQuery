namespace Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;

public class Gsd1ScriptFile
{
    public required IList<Gsd1ScriptInstruction> Instructions { get; set; }
    public required IList<Gsd1ScriptArgument> Arguments { get; set; }
}