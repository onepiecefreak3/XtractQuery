using Komponent.IO.Attributes;

namespace XtractQuery.Parsers.Models.Xseq
{
    class XseqHeader
    {
        [FixedLength(4)]
        public string magic = "XSEQ";

        public short table0EntryCount;
        public short table0Offset = 0x20 >> 2;

        public short table1Offset;
        public short table1EntryCount;

        public short table2Offset;
        public short table2EntryCount;

        public short table3Offset;
        public short table3EntryCount;

        public short unk3 = 0;
        public short table4Offset;
    }
}
