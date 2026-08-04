namespace Logic.Domain.Level5.Contract.DataClasses.Script;

public class ScriptFile
{
    public required IList<ScriptFunction> Functions { get; set; }
    public required IList<ScriptJump> Jumps { get; set; }
    public required IList<ScriptInstruction> Instructions { get; set; }
    public required IList<ScriptArgument> Arguments { get; set; }

    public PointerLength Length { get; set; }
}