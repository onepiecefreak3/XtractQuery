using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XtractQuery.Parser
{
    public class XseqCodeBlock
    {
        public int functionNameOffset;
        public short unk1;
        public short instructionOffset;
        public short instructionEndOffset;
        public short table1Offset;
        public short table1Count;
        public short unk2;
        public short unk3;
        public short unk4;
    }

    public class XseqAdditionalCodeBinding
    {
        public int nameOffset;
        public short unk1;
        public short instructionOffset;
    }

    public class XseqFuncStruct
    {
        public short varOffset;
        public short varCount;
        public short unk1;
        public short subroutine;
        public int zero0 = 0;
    }

    public class XseqVarStruct
    {
        public int type;        //bitshit by 1 to right to get real type
                                /* 0x0 - integer?
                                 * 0x1 - hash?
                                 * 0x2 - ?
                                 * 0xc - string -> command value is offset into table 3 then, to gather the string*/
        public uint value;
    }
}
