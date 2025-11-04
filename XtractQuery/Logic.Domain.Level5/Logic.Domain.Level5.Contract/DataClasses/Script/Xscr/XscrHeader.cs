namespace Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;

public class XscrHeader
{
    public string magic;
    public short instructionEntryCount;
    public ushort instructionOffset;
    public int argumentEntryCount;
    public int argumentOffset;
    public int stringOffset;
}