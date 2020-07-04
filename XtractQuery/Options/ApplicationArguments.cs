using System;
using System.Collections.Generic;

namespace XtractQuery.Options
{
    class ApplicationArguments
    {
        public bool NoArguments => !Help.Exists &&
                                   !Operation.Exists &&
                                   !QueryType.Exists &&
                                   !QueryFile.Exists;

        public Argument Help { get; private set; } = Argument.Empty;

        public Argument Operation { get; private set; } = Argument.Empty;

        public Argument QueryType { get; private set; } = Argument.Empty;

        public Argument QueryFile { get; private set; } = Argument.Empty;

        public ApplicationArguments(string[] args)
        {
            ParseInternal(args);
        }

        private void ParseInternal(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                // Assert parsing the next option
                AssertIsOption(args[i]);
                var option = args[i];

                // Collect its arguments
                var arguments = CollectArguments(args, i + 1, out var readValues);
                i += readValues;

                // Create argument object based on option
                switch (option)
                {
                    case "-h":
                    case "--help":
                        Help = new Argument(true);
                        break;

                    case "-o":
                    case "--operation":
                        Operation = new Argument(true, arguments);
                        break;

                    case "-t":
                    case "--type":
                        QueryType = new Argument(true, arguments);
                        break;

                    case "-f":
                    case "--file":
                        QueryFile = new Argument(true, arguments);
                        break;
                }
            }
        }

        private bool IsOptionValue(string arg)
        {
            return arg.StartsWith('-') || arg.StartsWith("--");
        }

        private IList<string> CollectArguments(string[] args, int start, out int readValues)
        {
            var result = new List<string>();

            var index = start;
            while (index < args.Length && !IsOptionValue(args[index]))
            {
                result.Add(args[index++]);
            }

            readValues = index - start;
            return result;
        }

        private void AssertIsOption(string arg)
        {
            if (!IsOptionValue(arg))
                throw new ArgumentException($"{arg} is no option. Type -h or --help to see what options are allowed.");
        }
    }
}
