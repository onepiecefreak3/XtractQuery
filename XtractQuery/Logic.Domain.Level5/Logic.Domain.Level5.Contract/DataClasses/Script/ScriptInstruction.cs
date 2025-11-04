namespace Logic.Domain.Level5.Contract.DataClasses.Script;

public class ScriptInstruction
{
    public short ArgumentIndex { get; set; }
    public short ArgumentCount { get; set; }

    public short ReturnParameter { get; set; }

    public short Type { get; set; }
}