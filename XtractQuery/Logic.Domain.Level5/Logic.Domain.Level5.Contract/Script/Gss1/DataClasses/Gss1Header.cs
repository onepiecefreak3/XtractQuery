namespace Logic.Domain.Level5.Contract.Script.Gss1.DataClasses;

public class Gss1Header
{
    public string magic;

    public short functionEntryCount;
    public ushort functionOffset;

    public ushort jumpOffset;
    public short jumpEntryCount;

    public ushort instructionOffset;
    public short instructionEntryCount;

    public ushort argumentOffset;
    public short argumentEntryCount;

    public short globalVariableCount;
    public ushort stringOffset;
}