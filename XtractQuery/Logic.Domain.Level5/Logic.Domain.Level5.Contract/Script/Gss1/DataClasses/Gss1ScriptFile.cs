using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Gss1.DataClasses;

public class Gss1ScriptFile
{
    public IList<ScriptFunction> Functions { get; set; }
    public IList<ScriptJump> Jumps { get; set; }
    public IList<ScriptInstruction> Instructions { get; set; }
    public IList<ScriptArgument> Arguments { get; set; }
}