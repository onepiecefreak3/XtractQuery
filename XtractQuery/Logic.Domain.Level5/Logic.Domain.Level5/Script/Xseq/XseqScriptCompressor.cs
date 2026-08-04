using Komponent.IO;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.Enums.Compression;
using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Domain.Level5.InternalContract.Compression;

namespace Logic.Domain.Level5.Script.Xseq;

internal class XseqScriptCompressor(ICompressor compressor) : IXseqScriptCompressor
{
    public void Compress(ScriptContainer container, Stream output, bool hasCompression)
    {
        Stream functionStream;
        Stream jumpStream;
        Stream instructionStream;
        Stream argumentStream;
        Stream stringStream;

        if (hasCompression)
        {
            functionStream = compressor.Compress(container.FunctionTable.Stream, CompressionType.Huffman8Bit);
            jumpStream = compressor.Compress(container.JumpTable.Stream, CompressionType.Huffman8Bit);
            instructionStream = compressor.Compress(container.InstructionTable.Stream, CompressionType.Lz10);
            argumentStream = compressor.Compress(container.ArgumentTable.Stream, CompressionType.Lz10);
            stringStream = compressor.Compress(container.StringTable.Stream, CompressionType.Lz10);
        }
        else
        {
            functionStream = container.FunctionTable.Stream;
            jumpStream = container.JumpTable.Stream;
            instructionStream = container.InstructionTable.Stream;
            argumentStream = container.ArgumentTable.Stream;
            stringStream = container.StringTable.Stream;

            functionStream.Position = 0;
            jumpStream.Position = 0;
            instructionStream.Position = 0;
            argumentStream.Position = 0;
            stringStream.Position = 0;
        }

        Write(container, output, functionStream, jumpStream, instructionStream, argumentStream, stringStream);
    }

    public void Compress(ScriptContainer container, Stream output, CompressionType compressionType)
    {
        Stream functionStream = compressor.Compress(container.FunctionTable.Stream, compressionType);
        Stream jumpStream = compressor.Compress(container.JumpTable.Stream, compressionType);
        Stream instructionStream = compressor.Compress(container.InstructionTable.Stream, compressionType);
        Stream argumentStream = compressor.Compress(container.ArgumentTable.Stream, compressionType);
        Stream stringStream = compressor.Compress(container.StringTable.Stream, compressionType);

        Write(container, output, functionStream, jumpStream, instructionStream, argumentStream, stringStream);
    }

    private void Write(ScriptContainer container, Stream output,
        Stream functionStream, Stream jumpStream, Stream instructionStream, Stream argumentStream, Stream stringStream)
    {
        long functionOffset = output.Position = 0x20;
        functionStream.CopyTo(output);

        long jumpOffset = output.Position = (output.Position + 3) & ~3;
        jumpStream.CopyTo(output);

        long instructionOffset = output.Position = (output.Position + 3) & ~3;
        instructionStream.CopyTo(output);

        long argumentOffset = output.Position = (output.Position + 3) & ~3;
        argumentStream.CopyTo(output);

        long stringOffset = output.Position = (output.Position + 3) & ~3;
        stringStream.CopyTo(output);

        using var writer = new BinaryWriterX(output);
        writer.WriteAlignment(4);

        output.Position = 0;
        writer.WriteString("XSEQ", writeNullTerminator: false);
        writer.Write((short)container.FunctionTable.EntryCount);
        writer.Write((ushort)(functionOffset >> 2));
        writer.Write((ushort)(jumpOffset >> 2));
        writer.Write((short)container.JumpTable.EntryCount);
        writer.Write((ushort)(instructionOffset >> 2));
        writer.Write((short)container.InstructionTable.EntryCount);
        writer.Write((ushort)(argumentOffset >> 2));
        writer.Write((short)container.ArgumentTable.EntryCount);
        writer.Write((short)container.GlobalVariableCount);
        writer.Write((ushort)(stringOffset >> 2));
    }
}