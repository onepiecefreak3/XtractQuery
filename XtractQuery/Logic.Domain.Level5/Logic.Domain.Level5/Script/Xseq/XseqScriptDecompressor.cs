using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Contract.Script.Xseq.DataClasses;
using Logic.Domain.Level5.Script.Xseq.InternalContract;

namespace Logic.Domain.Level5.Script.Xseq
{
    internal class XseqScriptDecompressor : ScriptDecompressor<XseqHeader>, IXseqScriptDecompressor
    {
        public XseqScriptDecompressor(IBinaryFactory binaryFactory, IBinaryTypeReader typeReader, IDecompressor decompressor)
            : base(binaryFactory, typeReader, decompressor)
        {
        }

        protected override int GetGlobalVariableCount(XseqHeader header)
        {
            return header.globalVariableCount;
        }

        protected override ScriptTable DecompressFunctions(Stream input, XseqHeader header)
        {
            return DecompressTable(input, header.functionOffset, header.functionEntryCount);
        }

        protected override ScriptTable DecompressJumps(Stream input, XseqHeader header)
        {
            return DecompressTable(input, header.jumpOffset, header.jumpEntryCount);
        }

        protected override ScriptTable DecompressInstructions(Stream input, XseqHeader header)
        {
            return DecompressTable(input, header.instructionOffset, header.instructionEntryCount);
        }

        protected override ScriptTable DecompressArguments(Stream input, XseqHeader header)
        {
            return DecompressTable(input, header.argumentOffset, header.argumentEntryCount);
        }

        protected override ScriptStringTable DecompressStrings(Stream input, XseqHeader header)
        {
            return DecompressStringTable(input, header.stringOffset);
        }
    }
}
