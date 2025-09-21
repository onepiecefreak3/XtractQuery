namespace Logic.Domain.Level5.Contract.Script.Gss1.DataClasses;

public class Gss1Function
{
    public int nameOffset;
    public ushort crc16;
    public short instructionOffset;
    public short instructionEndOffset;
    public short jumpOffset;
    public short jumpCount;
    public short localCount;
    public short objectCount;
    public short parameterCount;
}