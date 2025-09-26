using System.Text;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script.Gsd1;
using Logic.Domain.Level5.Contract.Script.Gsd1.DataClasses;

namespace Logic.Domain.Level5.Script.Gsd1;

class Gsd1ScriptWriter(IBinaryFactory binaryFactory, IGsd1ScriptComposer composer) : IGsd1ScriptWriter
{
    public void Write(Gsd1ScriptFile script, Stream output)
    {
        Gsd1ScriptContainer container = composer.Compose(script);

        Write(container, output);
    }

    public void Write(Gsd1ScriptContainer container, Stream output)
    {
        using IBinaryWriterX writer = binaryFactory.CreateWriter(output, true);

        output.Position = 0x10;
        WriteInstructions(container.Instructions, writer);
        writer.WriteAlignment(0x4);

        var argumentOffset = (int)output.Position;
        WriteArguments(container.Arguments, writer);
        writer.WriteAlignment(0x4);

        var stringOffset = (int)output.Position;
        container.Strings.Stream.Position = 0;
        container.Strings.Stream.CopyTo(output);
        writer.WriteAlignment(0x4);

        output.Position = 0;
        var header = new Gsd1Header
        {
            magic = "GSD1",
            instructionOffset = 0x10 >> 2,
            instructionEntryCount = (short)container.Instructions.Length,
            argumentOffset = (ushort)(argumentOffset >> 2),
            argumentEntryCount = (short)container.Arguments.Length,
            stringOffset = (ushort)(stringOffset >> 2)
        };
        WriteHeader(header, writer);
    }

    private void WriteHeader(Gsd1Header header, IBinaryWriterX writer)
    {
        writer.WriteString(header.magic, Encoding.ASCII, false, false);
        writer.Write(header.instructionEntryCount);
        writer.Write(header.instructionOffset);
        writer.Write(header.argumentEntryCount);
        writer.Write(header.argumentOffset);
        writer.Write(header.stringOffset);
    }

    private void WriteInstructions(Gsd1Instruction[] instructions, IBinaryWriterX writer)
    {
        foreach (Gsd1Instruction instruction in instructions)
            WriteInstruction(instruction, writer);
    }

    private void WriteInstruction(Gsd1Instruction instruction, IBinaryWriterX writer)
    {
        writer.Write(instruction.instructionType);
        writer.Write(instruction.argOffset);
        writer.Write(instruction.argCount);
    }

    private void WriteArguments(Gsd1Argument[] arguments, IBinaryWriterX writer)
    {
        foreach (Gsd1Argument argument in arguments)
            WriteArgument(argument, writer);
    }

    private void WriteArgument(Gsd1Argument argument, IBinaryWriterX writer)
    {
        writer.Write(argument.type);
        writer.Write(argument.value);
    }
}