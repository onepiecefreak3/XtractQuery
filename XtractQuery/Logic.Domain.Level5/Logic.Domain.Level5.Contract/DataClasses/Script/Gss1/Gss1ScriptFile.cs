namespace Logic.Domain.Level5.Contract.DataClasses.Script.Gss1;

public class Gss1ScriptFile
{
    public IList<ScriptFunction> Functions { get; set; }
    public IList<ScriptJump> Jumps { get; set; }
    public IList<ScriptInstruction> Instructions { get; set; }
    public IList<ScriptArgument> Arguments { get; set; }
}