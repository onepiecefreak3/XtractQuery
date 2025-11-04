namespace Logic.Domain.Level5.Contract.DataClasses.Script.Xseq;

public struct XseqFunction
{
    public long nameOffset;
    public ushort crc16;
    public short instructionOffset;
    public short instructionEndOffset;
    public short jumpOffset;
    public short jumpCount;
    public short localCount;
    public short objectCount;
    public short parameterCount;
}