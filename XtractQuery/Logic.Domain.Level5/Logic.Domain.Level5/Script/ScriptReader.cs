using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Script
{
    internal abstract class ScriptReader<TFunction, TJump, TInstruction, TArgument> : IScriptReader
    {
        private readonly IScriptDecompressor _decompressor;
        private readonly IBinaryFactory _binaryFactory;

        public ScriptReader(IScriptDecompressor decompressor, IBinaryFactory binaryFactory)
        {
            _decompressor = decompressor;
            _binaryFactory = binaryFactory;
        }

        public ScriptFile Read(Stream input)
        {
            ScriptContainer container = _decompressor.Decompress(input);

            return Read(container);
        }

        public ScriptFile Read(ScriptContainer container)
        {
            IList<ScriptFunction> functions = ReadFunctions(container.FunctionTable, container.StringTable);
            IList<ScriptJump> jumps = ReadJumps(container.JumpTable, container.StringTable);
            IList<ScriptInstruction> instructions = ReadInstructions(container.InstructionTable);
            IList<ScriptArgument> arguments = ReadArguments(container.ArgumentTable, container.StringTable);

            return new ScriptFile
            {
                Functions = functions,
                Jumps = jumps,
                Instructions = instructions,
                Arguments = arguments
            };
        }

        public IList<ScriptFunction> ReadFunctions(ScriptTable functionTable, ScriptStringTable? stringTable)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(functionTable.Stream, true);

            IReadOnlyList<TFunction> functions = ReadFunctions(functionTable.Stream, functionTable.EntryCount);

            return CreateFunctions(functions, stringTable);
        }

        public IList<ScriptJump> ReadJumps(ScriptTable jumpTable, ScriptStringTable? stringTable)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(jumpTable.Stream, true);

            IReadOnlyList<TJump> jumps = ReadJumps(jumpTable.Stream, jumpTable.EntryCount);

            return CreateJumps(jumps, stringTable);
        }

        public IList<ScriptInstruction> ReadInstructions(ScriptTable instructionTable)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(instructionTable.Stream, true);

            IReadOnlyList<TInstruction> instructions = ReadInstructions(instructionTable.Stream, instructionTable.EntryCount);

            return CreateInstructions(instructions);
        }

        public IList<ScriptArgument> ReadArguments(ScriptTable argumentTable, ScriptStringTable? stringTable)
        {
            using IBinaryReaderX br = _binaryFactory.CreateReader(argumentTable.Stream, true);

            IReadOnlyList<TArgument> arguments = ReadArguments(argumentTable.Stream, argumentTable.EntryCount);

            return CreateArguments(arguments, stringTable);
        }

        public abstract IReadOnlyList<TFunction> ReadFunctions(Stream functionStream, int entryCount);
        public abstract IReadOnlyList<TJump> ReadJumps(Stream jumpStream, int entryCount);
        public abstract IReadOnlyList<TInstruction> ReadInstructions(Stream instructionStream, int entryCount);
        public abstract IReadOnlyList<TArgument> ReadArguments(Stream argumentStream, int entryCount);

        protected IList<ScriptFunction> CreateFunctions(IReadOnlyList<TFunction> functions, ScriptStringTable? stringTable)
        {
            using IBinaryReaderX? stringReader = stringTable == null ? null : _binaryFactory.CreateReader(stringTable.Stream, true);

            var result = new ScriptFunction[functions.Count];

            var counter = 0;
            foreach (TFunction function in OrderFunctions(functions))
                result[counter++] = CreateFunction(function, stringReader);

            return result;
        }

        protected IList<ScriptJump> CreateJumps(IReadOnlyList<TJump> jumps, ScriptStringTable? stringTable = null)
        {
            using IBinaryReaderX? stringReader = stringTable == null ? null : _binaryFactory.CreateReader(stringTable.Stream, true);

            var result = new ScriptJump[jumps.Count];

            var counter = 0;
            foreach (TJump jump in jumps)
                result[counter++] = CreateJump(jump, stringReader);

            return result;
        }

        protected IList<ScriptInstruction> CreateInstructions(IReadOnlyList<TInstruction> instructions)
        {
            var result = new ScriptInstruction[instructions.Count];

            var counter = 0;
            foreach (TInstruction instruction in instructions)
                result[counter++] = CreateInstruction(instruction);

            return result;
        }

        protected IList<ScriptArgument> CreateArguments(IReadOnlyList<TArgument> arguments, ScriptStringTable? stringTable = null)
        {
            using IBinaryReaderX? stringReader = stringTable == null ? null : _binaryFactory.CreateReader(stringTable.Stream, true);

            var result = new ScriptArgument[arguments.Count];

            var counter = 0;
            foreach (TArgument argument in arguments)
                result[counter++] = CreateArgument(argument, stringReader);

            return result;
        }

        protected abstract ScriptFunction CreateFunction(TFunction function, IBinaryReaderX? stringReader);
        protected abstract ScriptJump CreateJump(TJump jump, IBinaryReaderX? stringReader);
        protected abstract ScriptInstruction CreateInstruction(TInstruction instruction);
        protected abstract ScriptArgument CreateArgument(TArgument argument, IBinaryReaderX? stringReader);

        protected abstract IEnumerable<TFunction> OrderFunctions(IReadOnlyList<TFunction> functions);
    }
}
