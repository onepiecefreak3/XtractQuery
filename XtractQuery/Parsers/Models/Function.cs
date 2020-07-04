using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using XtractQuery.Interfaces;
using StringSplitOptions = System.StringSplitOptions;

namespace XtractQuery.Parsers.Models
{
    class Function
    {
        private static readonly Regex ParseCheck = new Regex("^.*\\((\\$p\\d+, ?)*(\\$p\\d+)?\\).*{.*}$", RegexOptions.Singleline);

        public string Name { get; }

        public int ParameterCount { get; }

        public IList<Instruction> Instructions { get; }

        public IList<Jump> Jumps { get; }

        public IList<int> Unknowns { get; }

        public Function(string name, int parameterCount, IList<Jump> jumps, IList<Instruction> instructions, IList<int> unknowns)
        {
            Name = name;
            Instructions = instructions;
            Jumps = jumps;
            ParameterCount = parameterCount;
            Unknowns = unknowns;
        }

        public string GetString(IStringReader stringReader)
        {
            var lineValues = Instructions.Select(x => $"\t{x.GetString(stringReader)}").ToList();
            var offset = 0;
            foreach (var jump in Jumps)
            {
                var instructionIndex = Instructions.IndexOf(jump.Instruction);
                if (instructionIndex == -1)
                {
                    offset++;
                    lineValues.Add(jump.Label + ":");
                }
                else
                {
                    lineValues.Insert(instructionIndex + offset++, jump.Label + ":");
                }
            }

            var lines = string.Join(Environment.NewLine, lineValues);
            var parameters = string.Join(", ", Enumerable.Range(0, ParameterCount).Select(x => $"$p{x}"));
            var unknowns = string.Join(',', Unknowns);

            var functionParts = new[] { $"{Name}<{unknowns}>({parameters})", "{", lines, "}" };
            return string.Join(Environment.NewLine, functionParts);
        }

        public static Function Parse(string input, IStringWriter stringWriter)
        {
            if (!ParseCheck.IsMatch(input))
                return null;

            var functionName = GetFunctionName(input);
            var parameterCount = GetParameterCount(input);
            var unknowns = GetUnknowns(input);

            var functionBody = GetFunctionBody(input);
            var instructions = GetInstructions(functionBody)
                .Select(x => Instruction.Parse(x, stringWriter))
                .ToArray();

            var jumps = GetJumpLabels(functionBody)
                .Select(x => new Jump(x.label, x.index < instructions.Length ? instructions[x.index] : null))
                .ToArray();

            return new Function(functionName, parameterCount, jumps, instructions, unknowns);
        }

        private static string GetFunctionName(string input)
        {
            var index = input.IndexOf('<');
            return input.Substring(0, index);
        }

        private static IList<int> GetUnknowns(string input)
        {
            var index = input.IndexOf('<') + 1;
            var endIndex = input.IndexOf('>');

            var value = input.Substring(index, endIndex - index);

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.Parse(x.Trim(' ')))
                .ToArray();
        }

        private static int GetParameterCount(string input)
        {
            var index = input.IndexOf('(') + 1;
            var endIndex = input.IndexOf(')');

            var parameterValue = input.Substring(index, endIndex - index);

            if (string.IsNullOrEmpty(parameterValue))
                return 0;

            return parameterValue.Count(x => x == ',') + 1;
        }

        private static string GetFunctionBody(string input)
        {
            var index = input.IndexOf('{') + 1;
            var endIndex = input.LastIndexOf('}');

            return input.Substring(index, endIndex - index);
        }

        private static IEnumerable<string> GetInstructions(string functionBody)
        {
            var lines = functionBody.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            return lines.Where(x => x.StartsWith('\t')).Select(x => x.TrimStart('\t'));
        }

        private static IEnumerable<(string label, int index)> GetJumpLabels(string functionBody)
        {
            var lines = functionBody.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            var lineIndex = 0;
            foreach (var line in lines)
            {
                if (line.StartsWith('\t'))
                {
                    lineIndex++;
                    continue;
                }

                yield return (line.TrimEnd(':'), lineIndex);
            }
        }
    }
}
