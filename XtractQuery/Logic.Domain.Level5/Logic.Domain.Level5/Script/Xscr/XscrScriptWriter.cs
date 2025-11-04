using Komponent.IO;
using Logic.Domain.Level5.Contract.DataClasses.Script;
using Logic.Domain.Level5.Contract.DataClasses.Script.Xscr;
using Logic.Domain.Level5.Contract.Script.Xscr;

namespace Logic.Domain.Level5.Script.Xscr;

class XscrScriptWriter(IXscrScriptComposer composer, IXscrScriptCompressor compressor) : IXscrScriptWriter
{
    public void Write(XscrScriptFile script, Stream output)
    {
        XscrScriptContainer container = composer.Compose(script);

        Write(container, output);
    }

    public void Write(XscrScriptContainer container, Stream output)
    {
        XscrCompressionContainer compressionContainer = CreateContainer(container);

        compressor.Compress(compressionContainer, output);
    }

    private XscrCompressionContainer CreateContainer(XscrScriptContainer container)
    {
        Stream instructionStream = WriteInstructions(container.Instructions);
        Stream argumentStream = WriteArguments(container.Arguments);

        return new XscrCompressionContainer
        {
            InstructionTable = new CompressedScriptTable { Stream = instructionStream, EntryCount = container.Instructions.Length },
            ArgumentTable = new CompressedScriptTable { Stream = argumentStream, EntryCount = container.Arguments.Length },
            StringTable = container.Strings
        };
    }

    private Stream WriteInstructions(XscrInstruction[] instructions)
    {
        Stream instructionStream = new MemoryStream();
        using var writer = new BinaryWriterX(instructionStream, true);

        foreach (XscrInstruction instruction in instructions)
            WriteInstruction(instruction, writer);

        instructionStream.Position = 0;
        return instructionStream;
    }

    private void WriteInstruction(XscrInstruction instruction, BinaryWriterX writer)
    {
        writer.Write(instruction.instructionType);
        writer.Write(instruction.argCount);
        writer.Write(instruction.argOffset);
        writer.Write(instruction.zero);
    }

    private Stream WriteArguments(XscrArgument[] arguments)
    {
        Stream argumentStream = new MemoryStream();
        using var writer = new BinaryWriterX(argumentStream, true);

        foreach (XscrArgument argument in arguments)
            WriteArgument(argument, writer);

        argumentStream.Position = 0;
        return argumentStream;
    }

    private void WriteArgument(XscrArgument argument, BinaryWriterX writer)
    {
        writer.Write(argument.type);
        writer.Write(argument.value);
    }
}