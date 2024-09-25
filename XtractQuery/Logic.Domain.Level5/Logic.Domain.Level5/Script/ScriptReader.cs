using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;

namespace Logic.Domain.Level5.Script
{
    internal abstract class ScriptReader<TFunction, TJump, TInstruction, TArgument> : IScriptReader
    {
        private readonly IScriptDecompressor _decompressor;
        private readonly IScriptEntrySizeProvider _entrySizeProvider;
        private readonly IBinaryFactory _binaryFactory;

        public ScriptReader(IScriptDecompressor decompressor, IScriptEntrySizeProvider entrySizeProvider, IBinaryFactory binaryFactory)
        {
            _decompressor = decompressor;
            _entrySizeProvider = entrySizeProvider;
            _binaryFactory = binaryFactory;
        }

        public ScriptFile Read(Stream input)
        {
            ScriptContainer container = _decompressor.Decompress(input);

            return Read(container);
        }

        public ScriptFile Read(ScriptContainer container)
        {
            if (!TryDetectPointerLength(container, out PointerLength? length))
                throw new InvalidOperationException("Could not detect pointer length.");

            IList<ScriptFunction> functions = ReadFunctions(container.FunctionTable, container.StringTable, length!.Value);
            IList<ScriptJump> jumps = ReadJumps(container.JumpTable, container.StringTable, length!.Value);
            IList<ScriptInstruction> instructions = ReadInstructions(container.InstructionTable, length!.Value);
            IList<ScriptArgument> arguments = ReadArguments(container.ArgumentTable, instructions.AsReadOnly(), container.StringTable, length!.Value);

            return new ScriptFile
            {
                Functions = functions,
                Jumps = jumps,
                Instructions = instructions,
                Arguments = arguments,

                Length = length.Value
            };
        }

        public IList<ScriptFunction> ReadFunctions(ScriptTable functionTable, ScriptStringTable? stringTable)
        {
            if (!TryDetectTablePointerLength(functionTable, _entrySizeProvider.GetFunctionEntrySize, out PointerLength? length))
                throw new InvalidOperationException("Could not detect pointer length.");

            return ReadFunctions(functionTable, stringTable, length!.Value);
        }

        public IList<ScriptJump> ReadJumps(ScriptTable jumpTable, ScriptStringTable? stringTable)
        {
            if (!TryDetectTablePointerLength(jumpTable, _entrySizeProvider.GetJumpEntrySize, out PointerLength? length))
                throw new InvalidOperationException("Could not detect pointer length.");

            return ReadJumps(jumpTable, stringTable, length!.Value);
        }

        public IList<ScriptInstruction> ReadInstructions(ScriptTable instructionTable)
        {
            if (!TryDetectTablePointerLength(instructionTable, _entrySizeProvider.GetInstructionEntrySize, out PointerLength? length))
                throw new InvalidOperationException("Could not detect pointer length.");

            return ReadInstructions(instructionTable, length!.Value);
        }

        public IList<ScriptArgument> ReadArguments(ScriptTable argumentTable, ScriptTable instructionTable, ScriptStringTable? stringTable)
        {
            if (!TryDetectTablePointerLength(argumentTable, _entrySizeProvider.GetArgumentEntrySize, out PointerLength? length))
                throw new InvalidOperationException("Could not detect pointer length.");

            return ReadArguments(argumentTable, instructionTable, stringTable, length!.Value);
        }

        public abstract IReadOnlyList<TFunction> ReadFunctions(Stream functionStream, int entryCount, PointerLength length);
        public abstract IReadOnlyList<TJump> ReadJumps(Stream jumpStream, int entryCount, PointerLength length);
        public abstract IReadOnlyList<TInstruction> ReadInstructions(Stream instructionStream, int entryCount, PointerLength length);
        public abstract IReadOnlyList<TArgument> ReadArguments(Stream argumentStream, int entryCount, PointerLength length);

        public IList<ScriptFunction> CreateFunctions(IReadOnlyList<TFunction> functions, ScriptStringTable? stringTable)
        {
            using IBinaryReaderX? stringReader = stringTable == null ? null : _binaryFactory.CreateReader(stringTable.Stream, true);

            var result = new ScriptFunction[functions.Count];

            var counter = 0;
            foreach (TFunction function in OrderFunctions(functions))
                result[counter++] = CreateFunction(function, stringReader);

            return result;
        }

        public IList<ScriptJump> CreateJumps(IReadOnlyList<TJump> jumps, ScriptStringTable? stringTable = null)
        {
            using IBinaryReaderX? stringReader = stringTable == null ? null : _binaryFactory.CreateReader(stringTable.Stream, true);

            var result = new ScriptJump[jumps.Count];

            var counter = 0;
            foreach (TJump jump in jumps)
                result[counter++] = CreateJump(jump, stringReader);

            return result;
        }

        public IList<ScriptInstruction> CreateInstructions(IReadOnlyList<TInstruction> instructions)
        {
            var result = new ScriptInstruction[instructions.Count];

            var counter = 0;
            foreach (TInstruction instruction in instructions)
                result[counter++] = CreateInstruction(instruction);

            return result;
        }

        public IList<ScriptArgument> CreateArguments(IReadOnlyList<TArgument> arguments, IReadOnlyList<ScriptInstruction> instructions, ScriptStringTable? stringTable = null)
        {
            using IBinaryReaderX? stringReader = stringTable == null ? null : _binaryFactory.CreateReader(stringTable.Stream, true);

            var result = new ScriptArgument[arguments.Count];

            var instructionTypes = new (int, int)[arguments.Count];
            foreach (ScriptInstruction instruction in instructions)
            {
                for (var i = 0; i < instruction.ArgumentCount; i++)
                    instructionTypes[instruction.ArgumentIndex + i] = (instruction.Type, i);
            }

            var counter = 0;
            foreach (TArgument argument in arguments)
            {
                (int instructionType, int argumentIndex) = instructionTypes[counter];
                result[counter++] = CreateArgument(argument, instructionType, argumentIndex, stringReader);
            }

            return result;
        }

        protected abstract ScriptFunction CreateFunction(TFunction function, IBinaryReaderX? stringReader);
        protected abstract ScriptJump CreateJump(TJump jump, IBinaryReaderX? stringReader);
        protected abstract ScriptInstruction CreateInstruction(TInstruction instruction);
        protected abstract ScriptArgument CreateArgument(TArgument argument, int instructionType, int argumentIndex, IBinaryReaderX? stringReader);

        protected abstract IEnumerable<TFunction> OrderFunctions(IReadOnlyList<TFunction> functions);

        private IList<ScriptFunction> ReadFunctions(ScriptTable functionTable, ScriptStringTable? stringTable, PointerLength length)
        {
            IReadOnlyList<TFunction> functions = ReadFunctions(functionTable.Stream, functionTable.EntryCount, length);

            return CreateFunctions(functions, stringTable);
        }

        private IList<ScriptJump> ReadJumps(ScriptTable jumpTable, ScriptStringTable? stringTable, PointerLength length)
        {
            IReadOnlyList<TJump> jumps = ReadJumps(jumpTable.Stream, jumpTable.EntryCount, length);

            return CreateJumps(jumps, stringTable);
        }

        private IList<ScriptInstruction> ReadInstructions(ScriptTable instructionTable, PointerLength length)
        {
            IReadOnlyList<TInstruction> instructions = ReadInstructions(instructionTable.Stream, instructionTable.EntryCount, length);

            return CreateInstructions(instructions);
        }

        private IList<ScriptArgument> ReadArguments(ScriptTable argumentTable, ScriptTable instructionTable, ScriptStringTable? stringTable, PointerLength length)
        {
            IList<ScriptInstruction> instructions = ReadInstructions(instructionTable, length);

            return ReadArguments(argumentTable, instructions.AsReadOnly(), stringTable, length);
        }

        private IList<ScriptArgument> ReadArguments(ScriptTable argumentTable, IReadOnlyList<ScriptInstruction> instructions, ScriptStringTable? stringTable, PointerLength length)
        {
            IReadOnlyList<TArgument> arguments = ReadArguments(argumentTable.Stream, argumentTable.EntryCount, length);

            return CreateArguments(arguments, instructions, stringTable);
        }

        private bool TryDetectPointerLength(ScriptContainer container, out PointerLength? length)
        {
            length = null;

            for (var i = 0; i < 2; i++)
            {
                var localLength = (PointerLength)i;

                int entrySize = _entrySizeProvider.GetFunctionEntrySize(localLength);
                if (container.FunctionTable.EntryCount * entrySize != container.FunctionTable.Stream.Length)
                    continue;

                entrySize = _entrySizeProvider.GetJumpEntrySize(localLength);
                if (container.JumpTable.EntryCount * entrySize != container.JumpTable.Stream.Length)
                    continue;

                entrySize = _entrySizeProvider.GetInstructionEntrySize(localLength);
                if (container.InstructionTable.EntryCount * entrySize != container.InstructionTable.Stream.Length)
                    continue;

                entrySize = _entrySizeProvider.GetArgumentEntrySize(localLength);
                if (container.ArgumentTable.EntryCount * entrySize != container.ArgumentTable.Stream.Length)
                    continue;

                length = localLength;
                return true;
            }

            return false;
        }

        private bool TryDetectTablePointerLength(ScriptTable table, Func<PointerLength, int> getEntrySize, out PointerLength? length)
        {
            length = null;

            for (var i = 0; i < 2; i++)
            {
                var localLength = (PointerLength)i;

                int entrySize = getEntrySize(localLength);
                if (table.EntryCount * entrySize != table.Stream.Length)
                    continue;

                length = localLength;
                return true;
            }

            return false;
        }
    }
}
