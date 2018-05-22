using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XtractQuery.IO;

namespace XtractQuery.Parser
{
    public class Header
    {
        [Length(4)]
        public string Magic = "XQ32";
        public short table0EntryCount;
        public short table0Offset = 0x20 >> 2;
        public short table1Offset;
        public short table1EntryCount = 0;
        public short table2Offset;
        public short table2EntryCount;
        public short table3Offset;
        public short table3EntryCount;
        public short unk3 = 0;
        public short table4Offset;
    }

    public class CodeBlock
    {
        public int nameOffset;
        public uint hash;
        public short t2Offset;
        public short t2EndOffset;
        public short t1Offset = 0;
        public short t1EntryCount = 0;
        public short t1Offset2 = 0;
        public short t1Count2 = 0;
        public int unk7 = 0;
    }

    public class AdditionalCodeBinding
    {
        public int nameOffset;
        public uint hash;
        public int t2Offset;
    }

    public class FuncStruct
    {
        public short varOffset;
        public short varCount;
        public short unk1;
        public short subType;
        public int zero0 = 0;
    }

    public class VarStruct
    {
        public int type;        //bitshit by 1 to right to get real type
                                /* 0x0 - integer?
                                 * 0x1 - hash?
                                 * 0x2 - ?
                                 * 0xc - string -> command value is offset into table 3 then, to gather the string*/
        public uint value;
    }
}
