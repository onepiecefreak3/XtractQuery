namespace Logic.Domain.Level5.Contract.Script.Xq32.DataClasses;

public struct Xq32Function
{
    public long nameOffset;
    public uint crc32;
    public short instructionOffset;
    public short instructionEndOffset;
    public short jumpOffset;
    public short jumpCount;
    public short localCount;
    public short objectCount;
    public int parameterCount;
}