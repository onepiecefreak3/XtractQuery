using System.Text;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script.Gss1;
using Logic.Domain.Level5.Contract.Script.Gss1.DataClasses;

namespace Logic.Domain.Level5.Script.Gss1;

class Gss1ScriptWriter(IBinaryFactory binaryFactory, IGss1ScriptComposer composer) : IGss1ScriptWriter
{
    public void Write(Gss1ScriptFile script, Stream output)
    {
        Gss1ScriptContainer container = composer.Compose(script);

        Write(container, output);
    }

    public void Write(Gss1ScriptContainer container, Stream output)
    {
        using IBinaryWriterX writer = binaryFactory.CreateWriter(output, true);

        output.Position = 0x20;
        WriteFunctions(container.Functions, writer);
        writer.WriteAlignment(0x4);

        var jumpOffset = (int)output.Position;
        WriteJumps(container.Jumps, writer);
        writer.WriteAlignment(0x4);

        var instructionOffset = (int)output.Position;
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
        var header = new Gss1Header
        {
            magic = "GSS1",
            functionEntryCount = (short)container.Functions.Length,
            functionOffset = 0x20 >> 2,
            jumpOffset = (ushort)(jumpOffset >> 2),
            jumpEntryCount = (short)container.Jumps.Length,
            instructionOffset = (ushort)(instructionOffset >> 2),
            instructionEntryCount = (short)container.Instructions.Length,
            argumentOffset = (ushort)(argumentOffset >> 2),
            argumentEntryCount = (short)container.Arguments.Length,
            globalVariableCount = (short)container.GlobalVariableCount,
            stringOffset = (ushort)(stringOffset >> 2)
        };
        WriteHeader(header, writer);
    }

    private void WriteHeader(Gss1Header header, IBinaryWriterX writer)
    {
        writer.WriteString(header.magic, Encoding.ASCII, false, false);
        writer.Write(header.functionEntryCount);
        writer.Write(header.functionOffset);
        writer.Write(header.jumpOffset);
        writer.Write(header.jumpEntryCount);
        writer.Write(header.instructionOffset);
        writer.Write(header.instructionEntryCount);
        writer.Write(header.argumentOffset);
        writer.Write(header.argumentEntryCount);
        writer.Write(header.globalVariableCount);
        writer.Write(header.stringOffset);
    }

    private void WriteFunctions(Gss1Function[] functions, IBinaryWriterX writer)
    {
        foreach (Gss1Function function in functions)
            WriteFunction(function, writer);
    }

    private void WriteFunction(Gss1Function function, IBinaryWriterX writer)
    {
        writer.Write(function.nameOffset);
        writer.Write(function.crc16);
        writer.Write(function.instructionOffset);
        writer.Write(function.instructionEndOffset);
        writer.Write(function.jumpOffset);
        writer.Write(function.jumpCount);
        writer.Write(function.localCount);
        writer.Write(function.objectCount);
        writer.Write(function.parameterCount);
    }

    private void WriteJumps(Gss1Jump[] jumps, IBinaryWriterX writer)
    {
        foreach (Gss1Jump jump in jumps)
            WriteJump(jump, writer);
    }

    private void WriteJump(Gss1Jump jump, IBinaryWriterX writer)
    {
        writer.Write(jump.nameOffset);
        writer.Write(jump.crc16);
        writer.Write(jump.instructionIndex);
    }

    private void WriteInstructions(Gss1Instruction[] instructions, IBinaryWriterX writer)
    {
        foreach (Gss1Instruction instruction in instructions)
            WriteInstruction(instruction, writer);
    }

    private void WriteInstruction(Gss1Instruction instruction, IBinaryWriterX writer)
    {
        writer.Write(instruction.argOffset);
        writer.Write(instruction.argCount);
        writer.Write(instruction.returnParameter);
        writer.Write(instruction.instructionType);
        writer.Write(instruction.zero0);
    }

    private void WriteArguments(Gss1Argument[] arguments, IBinaryWriterX writer)
    {
        foreach (Gss1Argument argument in arguments)
            WriteArgument(argument, writer);
    }

    private void WriteArgument(Gss1Argument argument, IBinaryWriterX writer)
    {
        writer.Write(argument.type);
        writer.Write(argument.value);
    }
}