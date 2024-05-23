using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Script.Xseq.InternalContract;

namespace Logic.Domain.Level5.Script.Xseq
{
    internal class XseqScriptEntrySizeProvider : IXseqScriptEntrySizeProvider
    {
        public int GetFunctionEntrySize(PointerLength length)
        {
            switch (length)
            {
                case PointerLength.Int:
                    return 0x14;

                case PointerLength.Long:
                    return 0x18;

                default:
                    throw new InvalidOperationException($"Unknown pointer length {length}.");
            }
        }

        public int GetJumpEntrySize(PointerLength length)
        {
            switch (length)
            {
                case PointerLength.Int:
                    return 0x8;

                case PointerLength.Long:
                    return 0x10;

                default:
                    throw new InvalidOperationException($"Unknown pointer length {length}.");
            }
        }

        public int GetInstructionEntrySize(PointerLength length)
        {
            switch (length)
            {
                case PointerLength.Int:
                    return 0xC;

                case PointerLength.Long:
                    return 0x10;

                default:
                    throw new InvalidOperationException($"Unknown pointer length {length}.");
            }
        }

        public int GetArgumentEntrySize(PointerLength length)
        {
            switch (length)
            {
                case PointerLength.Int:
                    return 0x8;

                case PointerLength.Long:
                    return 0x10;

                default:
                    throw new InvalidOperationException($"Unknown pointer length {length}.");
            }
        }
    }
}
