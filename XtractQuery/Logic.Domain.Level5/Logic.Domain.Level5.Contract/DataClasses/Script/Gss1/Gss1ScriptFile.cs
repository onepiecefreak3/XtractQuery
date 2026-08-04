namespace Logic.Domain.Level5.Contract.DataClasses.Script.Gss1;

public class Gss1ScriptFile
{
    public required IList<ScriptFunction> Functions { get; set; }
    public required IList<ScriptJump> Jumps { get; set; }
    public required IList<ScriptInstruction> Instructions { get; set; }
    public required IList<ScriptArgument> Arguments { get; set; }
}