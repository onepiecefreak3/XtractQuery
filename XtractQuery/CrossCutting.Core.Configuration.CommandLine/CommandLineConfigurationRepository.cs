using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.Configuration.DataClasses;

namespace CrossCutting.Core.Configuration.CommandLine;

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
        var positionals = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            if (!IsOption(args[i]))
            {
                positionals.Add(args[i]);
                continue;
            }

            string name = GetOptionName(args[i]);
            if (TryTakeSingleArgument(args, i + 1, out string? argument))
            {
                category.AddEntry(name, argument);
                i++;
            }
            else
            {
                category.AddEntry(name, true);
            }
        }

        AssignTrailingFile(category, positionals);
        return category;
    }

    private static void AssignTrailingFile(ConfigCategory category, List<string> positionals)
    {
        if (positionals.Count == 0)
            return;

        if (positionals.Count > 1)
            throw new ArgumentException($"Unexpected arguments: {string.Join(", ", positionals)}");

        if (HasFileEntry(category))
            throw new ArgumentException($"Unexpected argument '{positionals[0]}': file was already specified with -f/--file.");

        category.AddEntry("f", positionals[0]);
    }

    private static bool HasFileEntry(ConfigCategory category)
    {
        return category.Entries.Any(e => e.Key is "f" or "file");
    }

    private static bool TryTakeSingleArgument(string[] args, int index, out string? argument)
    {
        if (index >= args.Length || IsOption(args[index]))
        {
            argument = null;
            return false;
        }

        argument = args[index];
        return true;
    }

    private static bool IsOption(string arg)
    {
        return arg.StartsWith("--") || arg.StartsWith('-');
    }

    private static string GetOptionName(string optionArg)
    {
        return optionArg.TrimStart('-');
    }
}
