using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using XtractQuery.Interfaces;

namespace XtractQuery.Parsers.Models
{
    class Instruction
    {
        private static readonly Regex ParseCheck = new Regex("\\$[pvxy]\\d+.*=.+\\(.*\\)");
        private static readonly Regex ArgumentSplit = new Regex(".*?<[\\d]+>");

        public int InstructionType { get; }

        public IList<Argument> Arguments { get; }

        public int ReturnParameter { get; }

        public Instruction(int instructionType, IList<Argument> arguments, int returnParameter)
        {
            InstructionType = instructionType;
            Arguments = arguments;
            ReturnParameter = returnParameter;
        }

        public string GetString(IStringReader stringReader)
        {
            var args = string.Join(", ", Arguments.Select(x => x.GetString(InstructionType, stringReader)));
            FunctionDictionaryParser.MergeIntoDictionary(RoutineMap);
            var subName = RoutineMap.ContainsKey(InstructionType) ? RoutineMap[InstructionType] : $"sub{InstructionType}";
            var returnParameter = Parameter.Parse(ReturnParameter);

            return $"{returnParameter.ParameterName} = {subName}({args})";
        }

        public static Instruction Parse(string input, IStringWriter stringWriter)
        {
            if (!ParseCheck.IsMatch(input))
                return null;

            var returnParameter = GetReturnParameter(input);
            var instructionType = GetInstructionType(input);

            var argumentBody = GetArgumentBody(input);
            var arguments = string.IsNullOrEmpty(argumentBody) ?
                Array.Empty<Argument>() :
                GetArguments(argumentBody)
                    .Select(x => Argument.Parse(instructionType, x, stringWriter))
                    .ToArray();

            return new Instruction(instructionType, arguments, returnParameter.ParameterValue);
        }

        private static int GetInstructionType(string input)
        {
            // Find start of instruction name after equal sign
            var instructionIndex = input.IndexOf("=") + 1;
            while (input[instructionIndex] == ' ')
                instructionIndex++;

            var instructionPart = input.Substring(instructionIndex, input.Length - instructionIndex);

            int endIndex;
            if (instructionPart.StartsWith("sub"))
            {
                // Parse sub number
                endIndex = instructionPart.IndexOf('(');
                return int.Parse(instructionPart.Substring(3, endIndex - 3));
            }

            endIndex = instructionPart.IndexOf('(');
            var instructionName = instructionPart.Substring(0, endIndex);
            if (RoutineMap.Values.Contains(instructionName))
            {
                return RoutineMap.First(x => x.Value == instructionName).Key;
            }

            return -1;
        }

        private static Parameter GetReturnParameter(string input)
        {
            var endIndex = input.IndexOf('=');

            var parameterName = input.Substring(0, endIndex).TrimEnd(' ');
            return Parameter.Parse(parameterName);
        }

        private static string GetArgumentBody(string input)
        {
            var index = input.IndexOf('(') + 1;
            var endIndex = input.LastIndexOf(')');

            return input.Substring(index, endIndex - index);
        }

        private static IEnumerable<string> GetArguments(string argumentBody)
        {
            var matches= ArgumentSplit.Matches(argumentBody);
            foreach (var matchValue in matches.Select(x=>x.Value))
            {
                if (matchValue.StartsWith(", "))
                {
                    yield return matchValue.Substring(2);
                    continue;
                }

                yield return matchValue;
            }
        }

        public static IDictionary<int, string> RoutineMap = new Dictionary<int, string>
        {
          
        };
    }
}
