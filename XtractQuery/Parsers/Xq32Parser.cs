using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using XtractQuery.Interfaces;
using XtractQuery.Parsers.Models;
using XtractQuery.Parsers.Models.Xq32;

namespace XtractQuery.Parsers
{
    class Xq32Parser : BaseParser
    {
        private IStringReader _stringReader;

        public Xq32Parser()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public override void Decompile(Stream input, Stream output)
        {
            var functions = ParseTables(input);

            using var streamWriter = new StreamWriter(output, Encoding.UTF8, -1, true);
            foreach (var function in functions)
            {
                streamWriter.WriteLine(function.GetString(_stringReader));
                streamWriter.WriteLine();
            }
        }

        public override void Compile(Stream input, Stream output)
        {
            var stringStream = new MemoryStream();
            var stringWriter = new StringWriter(stringStream);

            var functions = ParseText(input, stringWriter);
            var jumps = functions.SelectMany(x => x.Jumps).ToArray();
            var instructions = functions.SelectMany(x => x.Instructions).ToArray();
            var arguments = functions.SelectMany(x => x.Instructions.SelectMany(y => y.Arguments)).ToArray();

            var functionStream = WriteFunctions(functions, stringWriter);
            var jumpStream = WriteJumps(jumps, instructions, stringWriter);
            var instructionStream = WriteInstructions(instructions);
            var argumentStream = WriteArguments(arguments);

            var header = new Xq32Header
            {
                table0EntryCount = (short)functions.Count,
                table1EntryCount = (short)jumps.Length,
                table2EntryCount = (short)instructions.Length,
                table3EntryCount = (short)arguments.Length
            };

            output.Position = header.table0Offset << 2;
            Compress(functionStream).CopyTo(output);

            header.table1Offset = (short)((output.Position + 3) >> 2);
            output.Position = header.table1Offset << 2;
            Compress(jumpStream).CopyTo(output);

            header.table2Offset = (short)((output.Position + 3) >> 2);
            output.Position = header.table2Offset << 2;
            Compress(instructionStream).CopyTo(output);

            header.table3Offset = (short)((output.Position + 3) >> 2);
            output.Position = header.table3Offset << 2;
            Compress(argumentStream).CopyTo(output);

            header.table4Offset = (short)((output.Position + 3) >> 2);
            output.Position = header.table4Offset << 2;
            Compress(stringStream).CopyTo(output);

            output.Position = 0;
            using var bw = new BinaryWriterX(output, true);
            bw.WriteType(header);
        }

        private IList<Function> ParseTables(Stream input)
        {
            using var br = new BinaryReaderX(input, true);

            var header = br.ReadType<Xq32Header>();

            _stringReader = CreateStringReader(br, header);

            var arguments = ParseArguments(br, header);
            var instructions = ParseInstructions(br, header, arguments);
            var jumps = ParseJumps(br, header, instructions);

            return ParseFunctions(br, header, jumps, instructions);
        }

        private IList<Function> ParseText(Stream input, IStringWriter stringWriter)
        {
            var inputReader = new StreamReader(input);
            var content = inputReader.ReadToEnd();

            var functionValues = content.Split(Environment.NewLine + Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            return functionValues.Select(f => Function.Parse(f, stringWriter)).ToArray();
        }

        private IStringReader CreateStringReader(BinaryReaderX br, Xq32Header header)
        {
            var stringStream = Decompress(br.BaseStream, header.table4Offset << 2);

            return new StringReader(stringStream);
        }

        private IList<Argument> ParseArguments(BinaryReaderX br, Xq32Header header)
        {
            var argumentStream = Decompress(br.BaseStream, header.table3Offset << 2);

            var arguments = new BinaryReaderX(argumentStream).ReadMultiple<Xq32Argument>(header.table3EntryCount);
            return arguments.Select(x => new Argument(x.type, x.value)).ToArray();
        }

        private IList<Instruction> ParseInstructions(BinaryReaderX br, Xq32Header header, IList<Argument> arguments)
        {
            var instructionStream = Decompress(br.BaseStream, header.table2Offset << 2);
            var instructions = new BinaryReaderX(instructionStream).ReadMultiple<Xq32Instruction>(header.table2EntryCount);

            var result = new List<Instruction>();
            foreach (var instruction in instructions)
            {
                var instructionArguments = arguments
                    .Skip(instruction.argOffset)
                    .Take(instruction.argCount)
                    .ToArray();

                result.Add(new Instruction(instruction.subType, instructionArguments, instruction.returnParameter));
            }

            return result;
        }

        private IList<Function> ParseFunctions(BinaryReaderX br, Xq32Header header, IList<Jump> jumps, IList<Instruction> instructions)
        {
            var functionStream = Decompress(br.BaseStream, header.table0Offset << 2);
            var functions = new BinaryReaderX(functionStream).ReadMultiple<Xq32Function>(header.table0EntryCount);

            var result = new List<Function>();
            foreach (var function in functions)
            {
                var functionName = _stringReader.Read(function.nameOffset);
                var functionInstructions = instructions
                    .Skip(function.instructionOffset)
                    .Take(function.instructionEndOffset - function.instructionOffset)
                    .ToArray();

                var functionJumps = jumps
                    .Skip(function.jumpOffset)
                    .Take(function.jumpCount)
                    .ToArray();

                var functionUnknowns = new int[] { function.unk1, function.unk2 };

                result.Add(new Function(functionName, function.parameterCount, functionJumps, functionInstructions, functionUnknowns));
            }

            return result;
        }

        private IList<Jump> ParseJumps(BinaryReaderX br, Xq32Header header, IList<Instruction> instructions)
        {
            var jumpStream = Decompress(br.BaseStream, header.table1Offset << 2);
            var jumps = new BinaryReaderX(jumpStream).ReadMultiple<Xq32Jump>(header.table1EntryCount);

            var result = new List<Jump>();
            foreach (var jump in jumps)
            {
                var jumpInstruction = jump.instructionIndex < instructions.Count ? instructions[jump.instructionIndex] : null;
                result.Add(new Jump(_stringReader.Read(jump.nameOffset), jumpInstruction));
            }

            return result;
        }

        private Stream WriteFunctions(IList<Function> functions, IStringWriter stringWriter)
        {
            var functionStream = new MemoryStream();

            var instructionOffset = 0;
            var jumpOffset = 0;

            using var bw = new BinaryWriterX(functionStream, true);
            foreach (var function in functions)
            {
                bw.WriteType(new Xq32Function
                {
                    instructionOffset = (short)instructionOffset,
                    instructionEndOffset = (short)(instructionOffset + function.Instructions.Count),

                    nameOffset = (int)stringWriter.Write(function.Name),
                    crc32 = stringWriter.GetCrc32(function.Name),

                    jumpOffset = (short)jumpOffset,
                    jumpCount = (short)function.Jumps.Count,

                    parameterCount = (short)function.ParameterCount,

                    // TODO: Retrieve unknown values
                    unk1 = (short)function.Unknowns[0],
                    unk2 = (short)function.Unknowns[1]
                });

                instructionOffset += function.Instructions.Count;
                jumpOffset += function.Jumps.Count;
            }

            functionStream.Position = 0;
            return functionStream;
        }

        private Stream WriteJumps(IList<Jump> jumps, IList<Instruction> instructions, IStringWriter stringWriter)
        {
            var jumpStream = new MemoryStream();

            using var bw = new BinaryWriterX(jumpStream, true);
            foreach (var jump in jumps)
            {
                var instructionIndex = instructions.IndexOf(jump.Instruction);
                bw.WriteType(new Xq32Jump
                {
                    instructionIndex = (short)(instructionIndex == -1 ? instructions.Count : instructionIndex),
                    nameOffset = (int)stringWriter.Write(jump.Label),
                    crc32 = stringWriter.GetCrc32(jump.Label)
                });
            }

            jumpStream.Position = 0;
            return jumpStream;
        }

        private Stream WriteInstructions(IList<Instruction> instructions)
        {
            var instructionStream = new MemoryStream();

            var argumentOffset = 0;

            using var bw = new BinaryWriterX(instructionStream, true);
            foreach (var instruction in instructions)
            {
                bw.WriteType(new Xq32Instruction
                {
                    subType = (short)instruction.InstructionType,

                    argOffset = (short)argumentOffset,
                    argCount = (short)instruction.Arguments.Count,

                    returnParameter = (short)instruction.Unk1
                });

                argumentOffset += instruction.Arguments.Count;
            }

            instructionStream.Position = 0;
            return instructionStream;
        }

        private Stream WriteArguments(IList<Argument> arguments)
        {
            var argumentStream = new MemoryStream();

            using var bw = new BinaryWriterX(argumentStream, true);
            foreach (var argument in arguments)
            {
                bw.WriteType(new Xq32Argument
                {
                    type = argument.Type,
                    value = (uint)argument.Value
                });
            }

            argumentStream.Position = 0;
            return argumentStream;
        }
    }
}
