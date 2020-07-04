using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using XtractQuery.Interfaces;

namespace XtractQuery.Parsers.Models
{
    class Argument
    {
        private static readonly Regex ParseCheck = new Regex("\"?.*\"?<\\d+>");
        private static readonly Regex StringValue = new Regex("\"(.*)\"");
        private static readonly Regex IsParameter = new Regex("\\$p(\\d+)");

        public int Type { get; }

        public long Value { get; }

        public Argument(int type, long value)
        {
            Type = type;
            Value = value;
        }

        public string GetString(int subType, IStringReader stringReader)
        {
            string value;
            switch (Type)
            {
                // Integer
                case 1:
                    value = $"{Value}";
                    break;

                // Unsigned Integer
                case 2:
                    if (subType == 0x14 || subType == 0x1E || subType == 0x1F || subType == 0x21)
                    {
                        value = $"\"{stringReader.GetByHash((uint)Value)}\"";
                        break;
                    }

                    value = $"{Value}";
                    break;

                // Float
                case 3:
                    value = $"{BitConverter.Int32BitsToSingle((int)Value).ToString("0.00", CultureInfo.GetCultureInfo("en-gb"))}";
                    break;

                // Execution Context values
                case 4:
                    // 4000+ ?
                    // Values 3000+ are input parameters to the method
                    // 2000+ ?
                    // Values 1000+ are stack values
                    if (Value >= 3000 && Value <= 3999)
                    {
                        value = $"$p{Value - 3000}";
                        break;
                    }

                    value = $"{Value}";
                    break;

                // String
                case 24:
                    value = $"\"{stringReader.Read(Value)}\"";
                    break;

                // Method Name
                case 25:
                    value = $"\"{stringReader.Read(Value)}\"";
                    break;

                default:
                    throw new InvalidOperationException($"Unknown type {Type}.");
            }

            return value + $"<{Type}>";
        }

        // TODO: Try to detect type from value itself
        public static Argument Parse(int subType, string input, IStringWriter stringWriter)
        {
            if (!ParseCheck.IsMatch(input))
                return null;

            var type = GetArgumentType(input);
            var value = GetArgumentValue(input);

            switch (type)
            {
                case 1:
                    return new Argument(type, long.Parse(value));

                case 2:
                    if (subType != 0x14 && subType != 0x1E && subType != 0x1F && subType != 0x21)
                        return new Argument(type, long.Parse(value));

                    var stringValue = StringValue.Match(value).Groups.Values.Skip(1).First().Value;
                    stringWriter.Write(stringValue);
                    var stringHash = stringWriter.GetCrc32(stringValue);

                    return new Argument(type, stringHash);

                case 3:
                    return new Argument(type, BitConverter.SingleToInt32Bits(float.Parse(value, CultureInfo.GetCultureInfo("en-gb"))));

                case 4:
                    if (!IsParameter.IsMatch(value))
                        return new Argument(type, long.Parse(value));

                    var parameter = IsParameter.Match(value).Groups.Values.Skip(1).First().Value;
                    return new Argument(type, long.Parse(parameter) + 3000);

                case 24:
                    var stringValue1 = StringValue.Match(value).Groups.Values.Skip(1).First().Value;
                    return new Argument(type, stringWriter.Write(stringValue1));

                case 25:
                    var stringValue2 = StringValue.Match(value).Groups.Values.Skip(1).First().Value;
                    return new Argument(type, stringWriter.Write(stringValue2));

                default:
                    return null;
            }
        }

        private static int GetArgumentType(string input)
        {
            var index = input.LastIndexOf('<') + 1;
            var endIndex = input.LastIndexOf('>');

            return int.Parse(input.Substring(index, endIndex - index));
        }

        private static string GetArgumentValue(string input)
        {
            var index = input.LastIndexOf('<');

            return input.Substring(0, index);
        }
    }
}
