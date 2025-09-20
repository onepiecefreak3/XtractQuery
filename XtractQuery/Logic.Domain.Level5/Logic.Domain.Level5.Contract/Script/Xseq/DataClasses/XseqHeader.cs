using Logic.Domain.Kuriimu2.KomponentAdapter.Contract.DataClasses;

namespace Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;

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