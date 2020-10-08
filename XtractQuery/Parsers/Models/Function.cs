using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using XtractQuery.Interfaces;

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
            var lineValues = new List<string>();
            foreach (var instruction in Instructions)
            {
                // Get all jumps pointing to the current instruction
                var viableJumps = Jumps.Where(x => x.Instruction == instruction);

                // Add jump labels
                lineValues.AddRange(viableJumps.Select(x => x.GetString() + ":"));

                // Add instruction
                lineValues.Add("\t" + instruction.GetString(stringReader));
            }

            // Add jumps at end of function
            var endJumps = Jumps.Where(x => !Instructions.Contains(x.Instruction));
            lineValues.AddRange(endJumps.Select(x => x.GetString() + ":"));

            // Prepare other strings
            var lines = string.Join(Environment.NewLine, lineValues);
            var parameters = string.Join(", ", Enumerable.Range(0, ParameterCount).Select(x => $"$p{x}"));
            var unknowns = string.Join(',', Unknowns);

            // Build final function
            var functionParts = new[] { $"{Name}<{unknowns}>({parameters})" };
            var functionParts2 = !lines.Any() ? new[] { "{", "}" } : new[] { "{", lines, "}" };
            return string.Join(Environment.NewLine, functionParts.Concat(functionParts2));
        }

        public static IList<Function> ParseMultiple(string input, IStringWriter stringWriter)
        {
            // Split input to function parts
            var functionValues = input.Split(Environment.NewLine + Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            // Check if all functions match the pattern
            if (functionValues.Any(x => !ParseCheck.IsMatch(x)))
                return Array.Empty<Function>();

            // Write all function names
            foreach (var functionValue in functionValues.OrderBy(x => stringWriter.GetHash(GetFunctionName(x))))
                stringWriter.Write(GetFunctionName(functionValue));

            // Write all jump labels
            foreach (var functionValue in functionValues)
            {
                var functionBody = GetFunctionBody(functionValue);
                var jumps = GetJumpLabels(functionBody).OrderBy(x => stringWriter.GetHash(x.label));
                foreach (var jump in jumps)
                    stringWriter.Write(jump.label);
            }

            // Parse all functions
            return functionValues.Select(x => Parse(x, stringWriter)).ToArray();
        }

        private static Function Parse(string input, IStringWriter stringWriter)
        {
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
