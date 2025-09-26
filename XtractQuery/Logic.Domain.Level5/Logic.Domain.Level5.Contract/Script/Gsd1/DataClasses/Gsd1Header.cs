namespace Logic.Domain.Level5.Contract.Script.Gsd1.DataClasses;

public class Gsd1Header
{
    public string magic;
    public short instructionEntryCount;
    public ushort instructionOffset;
    public short argumentEntryCount;
    public ushort argumentOffset;
    public int stringOffset;
}