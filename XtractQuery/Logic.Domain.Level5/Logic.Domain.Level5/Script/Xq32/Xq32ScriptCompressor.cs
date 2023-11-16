using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xq32.DataClasses;
using Logic.Domain.Level5.Script.Xq32.InternalContract;

namespace Logic.Domain.Level5.Script.Xq32
{
    internal class Xq32ScriptCompressor : IXq32ScriptCompressor
    {
        private readonly IBinaryFactory _binaryFactory;
        private readonly IBinaryTypeWriter _typeWriter;
        private readonly ICompressor _compressor;

        public Xq32ScriptCompressor(IBinaryFactory binaryFactory, IBinaryTypeWriter typeWriter, ICompressor compressor)
        {
            _binaryFactory = binaryFactory;
            _typeWriter = typeWriter;
            _compressor = compressor;
        }

        public void Compress(ScriptContainer container, Stream output)
        {
            Stream functionStream = _compressor.Compress(container.FunctionTable.Stream, CompressionType.Huffman8Bit);
            Stream jumpStream = _compressor.Compress(container.JumpTable.Stream, CompressionType.Huffman8Bit);
            Stream instructionStream = _compressor.Compress(container.InstructionTable.Stream, CompressionType.Lz10);
            Stream argumentStream = _compressor.Compress(container.ArgumentTable.Stream, CompressionType.Lz10);
            Stream stringStream = _compressor.Compress(container.StringTable.Stream, CompressionType.Lz10);

            Write(container, output, functionStream, jumpStream, instructionStream, argumentStream, stringStream);
        }

        public void Compress(ScriptContainer container, Stream output, CompressionType compressionType)
        {
            Stream functionStream = _compressor.Compress(container.FunctionTable.Stream, compressionType);
            Stream jumpStream = _compressor.Compress(container.JumpTable.Stream, compressionType);
            Stream instructionStream = _compressor.Compress(container.InstructionTable.Stream, compressionType);
            Stream argumentStream = _compressor.Compress(container.ArgumentTable.Stream, compressionType);
            Stream stringStream = _compressor.Compress(container.StringTable.Stream, compressionType);

            Write(container, output, functionStream, jumpStream, instructionStream, argumentStream, stringStream);
        }

        private void Write(ScriptContainer container, Stream output,
            Stream functionStream, Stream jumpStream, Stream instructionStream, Stream argumentStream, Stream stringStream)
        {
            long functionOffset = output.Position = 0x20;
            functionStream.CopyTo(output);

            long jumpOffset = output.Position = output.Position + 3 & ~3;
            jumpStream.CopyTo(output);

            long instructionOffset = output.Position = output.Position + 3 & ~3;
            instructionStream.CopyTo(output);

            long argumentOffset = output.Position = output.Position + 3 & ~3;
            argumentStream.CopyTo(output);

            long stringOffset = output.Position = output.Position + 3 & ~3;
            stringStream.CopyTo(output);

            using IBinaryWriterX writer = _binaryFactory.CreateWriter(output);

            var header = new Xq32Header
            {
                magic = "XQ32",

                functionEntryCount = (short)container.FunctionTable.EntryCount,
                functionOffset = (ushort)(functionOffset >> 2),

                jumpEntryCount = (short)container.JumpTable.EntryCount,
                jumpOffset = (ushort)(jumpOffset >> 2),

                instructionEntryCount = (short)container.InstructionTable.EntryCount,
                instructionOffset = (ushort)(instructionOffset >> 2),

                argumentEntryCount = (short)container.ArgumentTable.EntryCount,
                argumentOffset = (ushort)(argumentOffset >> 2),

                stringOffset = (ushort)(stringOffset >> 2),

                globalVariableCount = (short)container.GlobalVariableCount
            };

            output.Position = 0;
            _typeWriter.Write(header, writer);
        }
    }
}
