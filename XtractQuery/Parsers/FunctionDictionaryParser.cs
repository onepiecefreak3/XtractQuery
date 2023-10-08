using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace XtractQuery.Parsers
{
    public class FunctionDictionaryParser
    {
        public static Dictionary<int, string> ParseDictionary()
        {
            string filePath = "FunctionDictionary.json";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("File not found: " + filePath);
                return new Dictionary<int, string>();
            }

            string json = File.ReadAllText(filePath);
            var jsonData = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            var formattedDictionary = new Dictionary<int, string>();

            foreach (var kvp in jsonData)
            {
                int subNumber = ExtractSubNumber(kvp.Key);
                if (subNumber != -1)
                {
                    formattedDictionary[subNumber] = kvp.Value;
                }
            }

            return formattedDictionary;
        }

        private static int ExtractSubNumber(string key)
        {
            var match = Regex.Match(key, @"sub(\d+)");
            return match.Success && match.Groups.Count > 1
                ? int.TryParse(match.Groups[1].Value, out int subNumber) ? subNumber : -1
                : -1;
        }



        public static void MergeIntoDictionary(IDictionary<int, string> targetDictionary)
        {
            var parsedDictionary = ParseDictionary();

            foreach (var kvp in parsedDictionary)
            {
                targetDictionary[kvp.Key] = kvp.Value;
            }
        }
    }
}
