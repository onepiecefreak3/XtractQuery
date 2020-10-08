using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace XtractQuery.Parsers.Models
{
    class Parameter
    {
        private static readonly Regex IsParameter = new Regex("^\\$[pvxy](\\d+)$");

        public int ParameterValue { get; }

        public string ParameterName { get; }

        private Parameter(int parameterValue, string parameterName)
        {
            ParameterValue = parameterValue;
            ParameterName = parameterName;
        }

        public static Parameter Parse(int parameterValue)
        {
            if (!TryParse(parameterValue, out var parameter))
                throw new InvalidOperationException($"The parameter value {parameterValue} could not be parsed.");

            return parameter;
        }

        public static Parameter Parse(string parameterName)
        {
            if (!TryParse(parameterName, out var parameter))
                throw new InvalidOperationException($"The parameter name {parameterName} could not be parsed.");

            return parameter;
        }

        public static bool TryParse(int parameterNumber, out Parameter parameter)
        {
            parameter = null;

            if (parameterNumber <= 999 || parameterNumber >= 5000)
                return false;

            var parameterName = "$";
            if (parameterNumber >= 1000 && parameterNumber <= 1999)
                parameterName += $"v{parameterNumber - 1000}";
            if (parameterNumber >= 2000 && parameterNumber <= 2999)
                parameterName += $"x{parameterNumber - 2000}";
            if (parameterNumber >= 3000 && parameterNumber <= 3999)
                parameterName += $"p{parameterNumber - 3000}";
            if (parameterNumber >= 4000 && parameterNumber <= 4999)
                parameterName += $"y{parameterNumber - 4000}";

            parameter = new Parameter(parameterNumber, parameterName);
            return true;
        }

        public static bool TryParse(string parameterName, out Parameter parameter)
        {
            parameter = null;

            if (!IsParameter.IsMatch(parameterName))
                return false;

            var parameterValue = int.Parse(IsParameter.Match(parameterName).Groups.Values.Skip(1).First().Value);
            var valueOffset = parameterName[1] == 'v' ? 1000 :
                parameterName[1] == 'x' ? 2000 :
                parameterName[1] == 'p' ? 3000 : 4000;

            parameter = new Parameter(parameterValue + valueOffset, parameterName);
            return true;
        }
    }
}
