namespace Logic.Domain.Level5.Contract.Script.Gsd1.DataClasses;

public class Gsd1ScriptFile
{
    public IList<Gsd1ScriptInstruction> Instructions { get; set; }
    public IList<Gsd1ScriptArgument> Arguments { get; set; }
}