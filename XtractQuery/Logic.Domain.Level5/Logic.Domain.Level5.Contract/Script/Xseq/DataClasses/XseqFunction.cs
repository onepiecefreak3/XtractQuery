namespace Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;

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