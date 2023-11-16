using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.Configuration.DataClasses;

namespace CrossCutting.Core.Configuration.CommandLine
{
    public class CommandLineConfigurationRepository : IConfigurationRepository
    {
        public IEnumerable<ConfigCategory> Load()
        {
            var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
            yield return CollectOptions(args);
        }

        private ConfigCategory CollectOptions(string[] args)
        {
            var category = new ConfigCategory { Name = "CommandLine" };

            for (var i = 0; i < args.Length; i++)
            {
                // Assert parsing the next option
                AssertIsOption(args[i]);
                string option = args[i];

                // Collect its arguments
                IList<string> arguments = CollectArguments(args, i + 1, out int readValues);
                i += readValues;

                string name = GetOptionName(option);
                if (readValues <= 0)
                    category.AddEntry(name, true);
                else if (readValues == 1)
                    category.AddEntry(name, arguments[0]);
                else
                    category.AddEntry(name, arguments);
            }

            return category;
        }

        private IList<string> CollectArguments(string[] args, int start, out int readValues)
        {
            var result = new List<string>();

            var index = start;
            while (index < args.Length && !IsOptionValue(args[index]))
                result.Add(args[index++]);

            readValues = index - start;
            return result;
        }

        private bool IsOptionValue(string arg)
        {
            return arg.StartsWith("--") || arg.StartsWith('-');
        }

        private void AssertIsOption(string arg)
        {
            if (!IsOptionValue(arg))
                throw new ArgumentException($"{arg} is not an option.");
        }

        private string GetOptionName(string optionArg)
        {
            return optionArg.TrimStart('-');
        }
    }
}