using System.Text;
using Komponent.IO;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;
using Logic.Domain.Level5.Contract.Enums.Compression;
using Logic.Domain.Level5.Contract.Script.Xscr;
using Logic.Domain.Level5.InternalContract.Compression;

namespace Logic.Domain.Level5.Script.Xscr;

internal class XscrScriptCompressor(ICompressor compressor) : IXscrScriptCompressor
{
    public void Compress(XscrCompressionContainer container, Stream output)
    {
        Stream instructionStream = container.InstructionTable.Stream;
        Stream argumentStream = container.ArgumentTable.Stream;
        Stream stringStream = container.StringTable.Stream;

        instructionStream = compressor.Compress(instructionStream, container.InstructionTable.CompressionType ?? CompressionType.Lz10);
        argumentStream = compressor.Compress(argumentStream, container.ArgumentTable.CompressionType ?? CompressionType.Lz10);
        stringStream = compressor.Compress(stringStream, container.StringTable.CompressionType ?? CompressionType.Lz10);

        Write(container, output, instructionStream, argumentStream, stringStream);
    }

    public void Compress(XscrCompressionContainer container, Stream output, CompressionType compressionType)
    {
        Stream instructionStream = compressor.Compress(container.InstructionTable.Stream, compressionType);
        Stream argumentStream = compressor.Compress(container.ArgumentTable.Stream, compressionType);
        Stream stringStream = compressor.Compress(container.StringTable.Stream, compressionType);

        Write(container, output, instructionStream, argumentStream, stringStream);
    }

    private void Write(XscrCompressionContainer container, Stream output, Stream instructionStream, Stream argumentStream, Stream stringStream)
    {
        long instructionOffset = output.Position = 0x14;
        instructionStream.CopyTo(output);

        long argumentOffset = output.Position = (output.Position + 3) & ~3;
        argumentStream.CopyTo(output);

        long stringOffset = output.Position = (output.Position + 3) & ~3;
        stringStream.CopyTo(output);

        using var writer = new BinaryWriterX(output, true);

        writer.WriteAlignment(4);

        var header = new XscrHeader
        {
            magic = "XSCR",

            instructionEntryCount = (short)container.InstructionTable.EntryCount,
            instructionOffset = (ushort)(instructionOffset >> 2),
            argumentEntryCount = container.ArgumentTable.EntryCount,
            argumentOffset = (int)(argumentOffset >> 2),
            stringOffset = (int)(stringOffset >> 2)
        };

        output.Position = 0;
        WriteHeader(header, writer);
    }

    private void WriteHeader(XscrHeader header, BinaryWriterX writer)
    {
        writer.WriteString(header.magic, Encoding.ASCII, false, false);
        writer.Write(header.instructionEntryCount);
        writer.Write(header.instructionOffset);
        writer.Write(header.argumentEntryCount);
        writer.Write(header.argumentOffset);
        writer.Write(header.stringOffset);
    }
}