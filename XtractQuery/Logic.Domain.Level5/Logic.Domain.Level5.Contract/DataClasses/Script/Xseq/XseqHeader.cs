using Komponent.Contract.Aspects;

namespace Logic.Domain.Level5.Contract.DataClasses.Script.Xseq;

public class XseqHeader
{
    [FixedLength(4)]
    public string magic = "XSEQ";

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