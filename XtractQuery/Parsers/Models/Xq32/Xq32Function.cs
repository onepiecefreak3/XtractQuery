namespace XtractQuery.Parsers.Models.Xq32
{
    class Xq32Function
    {
        public int nameOffset;
        public uint crc32;
        public short instructionOffset;
        public short instructionEndOffset;
        public short jumpOffset;
        public short jumpCount;
        public short unk1;
        public short unk2;
        public int parameterCount;
    }
}
