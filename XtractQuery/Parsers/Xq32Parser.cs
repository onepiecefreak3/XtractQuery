using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Komponent.IO;
using XtractQuery.Interfaces;
using XtractQuery.Parsers.Models;
using XtractQuery.Parsers.Models.Xq32;
using XtractQuery.Parsers.StringWriter;

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
            var functions = ParseTables(input, out var headerUnk);

            using var streamWriter = new StreamWriter(output, Encoding.UTF8, -1, true);
            streamWriter.WriteLine($"{{{headerUnk}}}");
            foreach (var function in functions)
            {
                streamWriter.WriteLine(function.GetString(_stringReader));
                streamWriter.WriteLine();
            }
        }

        public override void Compile(Stream input, Stream output)
        {
            var stringStream = new MemoryStream();
            var stringWriter = new Xq32StringWriter(stringStream);

            var content = new StreamReader(input).ReadToEnd();

            var unkRegex = new Regex("^{(\\d+)}");
            if (!unkRegex.IsMatch(content))
                throw new InvalidOperationException("Unknown header value is not prefixed to the script.");

            var headerUnk = short.Parse(unkRegex.Match(content).Groups[1].Value);
            var functions = Function.ParseMultiple(string.Join(Environment.NewLine, content.Split(Environment.NewLine).Skip(1)), stringWriter);
            var jumps = functions.SelectMany(x => x.Jumps.OrderBy(y => stringWriter.GetHash(y.Label))).ToArray();
            var instructions = functions.SelectMany(x => x.Instructions).ToArray();
            var arguments = functions.SelectMany(x => x.Instructions.SelectMany(y => y.Arguments)).ToArray();

            var functionStream = WriteFunctions(functions, jumps, instructions, stringWriter);
            var jumpStream = WriteJumps(jumps, functions, instructions, stringWriter);
            var instructionStream = WriteInstructions(instructions);
            var argumentStream = WriteArguments(arguments);

            var header = new Xq32Header
            {
                table0EntryCount = (short)functions.Count,
                table1EntryCount = (short)jumps.Length,
                table2EntryCount = (short)instructions.Length,
                table3EntryCount = (short)arguments.Length,
                unk3 = headerUnk
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

        private IList<Function> ParseTables(Stream input, out int headerUnk)
        {
            using var br = new BinaryReaderX(input, true);

            var header = br.ReadType<Xq32Header>();
            headerUnk = header.unk3;

            _stringReader = CreateStringReader(br, header);

            var arguments = ParseArguments(br, header);
            var instructions = ParseInstructions(br, header, arguments);
            var jumps = ParseJumps(br, header, instructions);

            return ParseFunctions(br, header, jumps, instructions);
        }

        private IStringReader CreateStringReader(BinaryReaderX br, Xq32Header header)
        {
            var stringStream = Decompress(br.BaseStream, header.table4Offset << 2);

            return new StringReader(stringStream);
        }

        #region Parse Methods

        private IList<Function> ParseFunctions(BinaryReaderX br, Xq32Header header, IList<Jump> jumps, IList<Instruction> instructions)
        {
            var functionStream = Decompress(br.BaseStream, header.table0Offset << 2);
            var functions = new BinaryReaderX(functionStream).ReadMultiple<Xq32Function>(header.table0EntryCount);

            var result = new List<Function>();
            foreach (var function in functions.OrderBy(x => x.instructionOffset))
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

        private IList<Argument> ParseArguments(BinaryReaderX br, Xq32Header header)
        {
            var argumentStream = Decompress(br.BaseStream, header.table3Offset << 2);
            var arguments = new BinaryReaderX(argumentStream).ReadMultiple<Xq32Argument>(header.table3EntryCount);

            return arguments.Select(x => new Argument(x.type, x.value)).ToArray();
        }

        #endregion

        #region Write Methods

        private Stream WriteFunctions(IList<Function> functions, IList<Jump> jumps, IList<Instruction> instructions, IStringWriter stringWriter)
        {
            var functionStream = new MemoryStream();

            var lastInstructionOffset = 0;
            var lastJumpOffset = 0;

            var convertedFunctions = new List<Xq32Function>();
            foreach (var function in functions)
            {
                var currentInstructionOffset = instructions.IndexOf(function.Instructions.FirstOrDefault());
                if (currentInstructionOffset >= 0)
                    lastInstructionOffset = currentInstructionOffset;

                var currentJumpOffset = jumps.IndexOf(function.Jumps.OrderBy(x => stringWriter.GetHash(x.Label)).FirstOrDefault());
                if (currentJumpOffset >= 0)
                    lastJumpOffset = currentJumpOffset;

                convertedFunctions.Add(new Xq32Function
                {
                    instructionOffset = (short)lastInstructionOffset,
                    instructionEndOffset = (short)(lastInstructionOffset + function.Instructions.Count),

                    nameOffset = (int)stringWriter.Write(function.Name),
                    crc32 = stringWriter.GetHash(function.Name),

                    jumpOffset = (short)lastJumpOffset,
                    jumpCount = (short)function.Jumps.Count,

                    parameterCount = (short)function.ParameterCount,

                    unk1 = (short)function.Unknowns[0],
                    unk2 = (short)function.Unknowns[1]
                });

                lastInstructionOffset += function.Instructions.Count;
                lastJumpOffset += function.Jumps.Count;
            }

            using var bw = new BinaryWriterX(functionStream, true);
            bw.WriteMultiple(convertedFunctions.OrderBy(x => x.crc32));

            functionStream.Position = 0;
            return functionStream;
        }

        private Stream WriteJumps(IList<Jump> jumps, IList<Function> functions, IList<Instruction> instructions, IStringWriter stringWriter)
        {
            var jumpStream = new MemoryStream();

            using var bw = new BinaryWriterX(jumpStream, true);
            foreach (var jump in jumps)
            {
                var relatedFunction = functions.First(x => x.Jumps.Contains(jump));
                var lastInstruction = relatedFunction.Instructions.Last();

                var instructionIndex = instructions.IndexOf(jump.Instruction);
                bw.WriteType(new Xq32Jump
                {
                    instructionIndex = instructionIndex == -1 ? instructions.IndexOf(lastInstruction) + 1 : instructionIndex,
                    nameOffset = (int)stringWriter.Write(jump.Label),
                    crc32 = stringWriter.GetHash(jump.Label)
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

                    returnParameter = (short)instruction.ReturnParameter
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

        #endregion
    }
}
