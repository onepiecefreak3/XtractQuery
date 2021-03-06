﻿using Komponent.IO.Attributes;

namespace XtractQuery.Parsers.Models.Xq32
{
    class Xq32Header
    {
        [FixedLength(4)]
        public string magic = "XQ32";

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
