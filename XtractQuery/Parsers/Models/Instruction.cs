using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using XtractQuery.Interfaces;

namespace XtractQuery.Parsers.Models
{
    // TODO: Print and evaluate returnParameter (Unk1) correctly
    class Instruction
    {
        private static readonly Regex ParseCheck = new Regex(".+\\(.*\\)");
        private static readonly Regex ArgumentSplit = new Regex(", *");

        public int InstructionType { get; }

        public IList<Argument> Arguments { get; }

        public int Unk1 { get; }

        public Instruction(int instructionType, IList<Argument> arguments, int unk1)
        {
            InstructionType = instructionType;
            Arguments = arguments;
            Unk1 = unk1;
        }

        public string GetString(IStringReader stringReader)
        {
            var args = string.Join(", ", Arguments.Select(x => x.GetString(InstructionType, stringReader)));
            var subName = RoutineMap.ContainsKey(InstructionType) ? RoutineMap[InstructionType] : $"sub{InstructionType}";

            return $"{subName}<{Unk1}>({args})";
        }

        public static Instruction Parse(string input, IStringWriter stringWriter)
        {
            if (!ParseCheck.IsMatch(input))
                return null;

            var instructionType = GetInstructionType(input);
            var unkValue = int.Parse(GetUnknownValue(input));

            var argumentBody = GetArgumentBody(input);
            var arguments = string.IsNullOrEmpty(argumentBody) ?
                Array.Empty<Argument>() :
                GetArguments(argumentBody)
                    .Select(x => Argument.Parse(instructionType, x, stringWriter))
                    .ToArray();

            return new Instruction(instructionType, arguments, unkValue);
        }

        private static int GetInstructionType(string input)
        {
            int endIndex;
            if (input.StartsWith("sub"))
            {
                endIndex = input.IndexOf('<');
                return int.Parse(input.Substring(3, endIndex - 3));
            }

            endIndex = input.IndexOf('<');
            var instructionName = input.Substring(0, endIndex);
            if (RoutineMap.Values.Contains(instructionName))
            {
                return RoutineMap.First(x => x.Value == instructionName).Key;
            }

            return -1;
        }

        private static string GetUnknownValue(string input)
        {
            var index = input.IndexOf('<') + 1;
            var endIndex = input.IndexOf('>');

            return input.Substring(index, endIndex - index);
        }

        private static string GetArgumentBody(string input)
        {
            var index = input.IndexOf('(') + 1;
            var endIndex = input.LastIndexOf(')');

            return input.Substring(index, endIndex - index);
        }

        private static IEnumerable<string> GetArguments(string argumentBody)
        {
            return ArgumentSplit.Split(argumentBody);
        }

        private static IDictionary<int, string> RoutineMap = new Dictionary<int, string>
        {
            [20] = "Call",
            [30] = "Jump1",
            [31] = "Jump2",
            [33] = "JumpParameter",
            [130] = "Compare"
        };
    }
}
