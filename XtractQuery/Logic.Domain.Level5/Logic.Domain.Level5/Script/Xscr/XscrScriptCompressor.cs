using System.Text;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Contract.Compression.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xscr;
using Logic.Domain.Level5.Contract.Script.Xscr.DataClasses;

namespace Logic.Domain.Level5.Script.Xscr;

internal class XscrScriptCompressor(IBinaryFactory binaryFactory, ICompressor compressor) : IXscrScriptCompressor
{
    public void Compress(XscrCompressionContainer container, Stream output)
    {
        Stream instructionStream = container.InstructionTable.Stream;
        Stream argumentStream = container.ArgumentTable.Stream;
        Stream stringStream = container.StringTable.Stream;

        instructionStream.Position = 0;
        argumentStream.Position = 0;
        stringStream.Position = 0;

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

        long argumentOffset = output.Position = output.Position + 3 & ~3;
        argumentStream.CopyTo(output);

        long stringOffset = output.Position = output.Position + 3 & ~3;
        stringStream.CopyTo(output);

        using IBinaryWriterX writer = binaryFactory.CreateWriter(output, true);

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

    private void WriteHeader(XscrHeader header, IBinaryWriterX writer)
    {
        writer.WriteString(header.magic, Encoding.ASCII, false, false);
        writer.Write(header.instructionEntryCount);
        writer.Write(header.instructionOffset);
        writer.Write(header.argumentEntryCount);
        writer.Write(header.argumentOffset);
        writer.Write(header.stringOffset);
    }
}