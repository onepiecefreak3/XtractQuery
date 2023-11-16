using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xq32.DataClasses;
using Logic.Domain.Level5.Script.Xq32.InternalContract;

namespace Logic.Domain.Level5.Script.Xq32
{
    internal class Xq32ScriptDecompressor : ScriptDecompressor<Xq32Header>, IXq32ScriptDecompressor
    {
        public Xq32ScriptDecompressor(IBinaryFactory binaryFactory, IBinaryTypeReader typeReader, IDecompressor decompressor)
            : base(binaryFactory, typeReader, decompressor)
        {
        }

        protected override int GetGlobalVariableCount(Xq32Header header)
        {
            return header.globalVariableCount;
        }

        protected override ScriptTable DecompressFunctions(Stream input, Xq32Header header)
        {
            return DecompressTable(input, header.functionOffset, header.functionEntryCount);
        }

        protected override ScriptTable DecompressJumps(Stream input, Xq32Header header)
        {
            return DecompressTable(input, header.jumpOffset, header.jumpEntryCount);
        }

        protected override ScriptTable DecompressInstructions(Stream input, Xq32Header header)
        {
            return DecompressTable(input, header.instructionOffset, header.instructionEntryCount);
        }

        protected override ScriptTable DecompressArguments(Stream input, Xq32Header header)
        {
            return DecompressTable(input, header.argumentOffset, header.argumentEntryCount);
        }

        protected override ScriptStringTable DecompressStrings(Stream input, Xq32Header header)
        {
            return DecompressStringTable(input, header.stringOffset);
        }
    }
}
