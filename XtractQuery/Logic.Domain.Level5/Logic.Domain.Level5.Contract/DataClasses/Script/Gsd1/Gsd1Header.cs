namespace Logic.Domain.Level5.Contract.DataClasses.Script.Gsd1;

public struct Gsd1Header
{
    public string magic;
    public short instructionEntryCount;
    public ushort instructionOffset;
    public short argumentEntryCount;
    public ushort argumentOffset;
    public int stringOffset;
}