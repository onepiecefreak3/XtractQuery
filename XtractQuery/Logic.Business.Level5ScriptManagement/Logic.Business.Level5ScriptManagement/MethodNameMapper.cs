using CrossCutting.Core.Contract.Serialization;
using Logic.Business.Level5ScriptManagement.InternalContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.Business.Level5ScriptManagement
{
    internal class MethodNameMapper : IMethodNameMapper
    {
        private readonly IDictionary<int, string> _methodNameMapping;
        private readonly IDictionary<string, int> _instructionTypeMapping;

        public MethodNameMapper(Level5ScriptManagementConfiguration config, ISerializer serializer)
        {
            _methodNameMapping = InitializeMapping(config.MethodMappingPath, serializer);
            _instructionTypeMapping = _methodNameMapping.ToDictionary(x => x.Value, y => y.Key);
        }

        public bool MapsInstructionType(int instructionType)
        {
            return _methodNameMapping.ContainsKey(instructionType);
        }

        public bool MapsMethodName(string methodName)
        {
            return _instructionTypeMapping.ContainsKey(methodName);
        }

        public string GetMethodName(int instructionType)
        {
            if (!_methodNameMapping.TryGetValue(instructionType, out string? methodName))
                throw new InvalidOperationException($"Instruction type {instructionType} is not mapped.");

            return methodName;
        }

        public int GetInstructionType(string methodName)
        {
            if (!_instructionTypeMapping.TryGetValue(methodName, out int instructionType))
                throw new InvalidOperationException($"Method name {methodName} is not mapped.");

            return instructionType;

            //return GetNumberFromStringEnd(methodName, out _);
        }

        private IDictionary<int, string> InitializeMapping(string mappingPath, ISerializer serializer)
        {
            mappingPath = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath), mappingPath);
            if (!File.Exists(mappingPath))
                return new Dictionary<int, string>();

            string mappingJson = File.ReadAllText(mappingPath);
            return serializer.Deserialize<IDictionary<int, string>>(mappingJson);
        }

        //private int GetNumberFromStringEnd(string text, out int startIndex)
        //{
        //    startIndex = text.Length;
        //    while (text[startIndex - 1] >= '0' && text[startIndex - 1] <= '9')
        //        startIndex--;

        //    return int.Parse(text[startIndex..]);
        //}
    }
}
