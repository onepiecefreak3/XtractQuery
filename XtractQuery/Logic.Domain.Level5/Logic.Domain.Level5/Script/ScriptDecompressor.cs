using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Script
{
    internal abstract class ScriptDecompressor<THeader> : IScriptDecompressor
    {
        private readonly IBinaryFactory _binaryFactory;
        private readonly IBinaryTypeReader _typeReader;
        private readonly IDecompressor _decompressor;

        public ScriptDecompressor(IBinaryFactory binaryFactory, IBinaryTypeReader typeReader, IDecompressor decompressor)
        {
            _binaryFactory = binaryFactory;
            _typeReader = typeReader;
            _decompressor = decompressor;
        }

        public ScriptContainer Decompress(Stream input)
        {
            THeader header = ReadHeader(input);

            return new ScriptContainer
            {
                GlobalVariableCount = GetGlobalVariableCount(header),

                FunctionTable = DecompressFunctions(input, header),
                JumpTable = DecompressJumps(input, header),
                InstructionTable = DecompressInstructions(input, header),
                ArgumentTable = DecompressArguments(input, header),
                StringTable = DecompressStrings(input, header)
            };
        }

        public int GetGlobalVariableCount(Stream input)
        {
            THeader header = ReadHeader(input);

            return GetGlobalVariableCount(header);
        }

        public ScriptTable DecompressFunctions(Stream input)
        {
            THeader header = ReadHeader(input);

            return DecompressFunctions(input, header);
        }

        public ScriptTable DecompressJumps(Stream input)
        {
            THeader header = ReadHeader(input);

            return DecompressJumps(input, header);
        }

        public ScriptTable DecompressInstructions(Stream input)
        {
            THeader header = ReadHeader(input);

            return DecompressInstructions(input, header);
        }

        public ScriptTable DecompressArguments(Stream input)
        {
            THeader header = ReadHeader(input);

            return DecompressArguments(input, header);
        }

        public ScriptStringTable DecompressStrings(Stream input)
        {
            THeader header = ReadHeader(input);

            return DecompressStrings(input, header);
        }

        protected abstract int GetGlobalVariableCount(THeader header);

        protected abstract ScriptTable DecompressFunctions(Stream input, THeader header);

        protected abstract ScriptTable DecompressJumps(Stream input, THeader header);

        protected abstract ScriptTable DecompressInstructions(Stream input, THeader header);

        protected abstract ScriptTable DecompressArguments(Stream input, THeader header);

        protected abstract ScriptStringTable DecompressStrings(Stream input, THeader header);

        protected ScriptTable DecompressTable(Stream input, int headerOffset, int headerEntryCount)
        {
            Stream decompressedStream = Decompress(input, headerOffset, out CompressionType compressionType);

            return new ScriptTable
            {
                EntryCount = headerEntryCount,
                CompressionType = compressionType,
                Stream = decompressedStream
            };
        }

        protected ScriptStringTable DecompressStringTable(Stream input, int headerOffset)
        {
            Stream decompressedStream = Decompress(input, headerOffset, out CompressionType compressionType);

            return new ScriptStringTable
            {
                CompressionType = compressionType,
                Stream = decompressedStream
            };
        }

        protected Stream Decompress(Stream input, int headerOffset, out CompressionType compressionType)
        {
            int offset = headerOffset << 2;

            compressionType = _decompressor.PeekCompressionType(input, offset);
            return _decompressor.Decompress(input, offset);
        }

        private THeader ReadHeader(Stream input)
        {
            long bkPos = input.Position;
            input.Position = 0;

            using IBinaryReaderX br = _binaryFactory.CreateReader(input, true);

            var header = _typeReader.Read<THeader>(br)!;
            input.Position = bkPos;

            return header;
        }
    }
}
