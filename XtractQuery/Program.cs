using System;
using System.IO;
using System.Linq;
using System.Reflection;
using XtractQuery.Interfaces;
using XtractQuery.Options;
using XtractQuery.Parsers;

namespace XtractQuery
{
    class Program
    {
        static void Main(string[] args)
        {
            var arguments = new ApplicationArguments(args);

            string operation;
            string type;
            string file;

            #region Argument checking

            if (arguments.NoArguments || arguments.Help.Exists)
            {
                PrintHelp();
                return;
            }

            if (!arguments.Operation.Exists)
            {
                Console.WriteLine("No operation mode was given. Specify an operation mode by using the -o argument.");
                return;
            }

            if (!arguments.QueryType.Exists)
            {
                Console.WriteLine("No query type was given. Specify a query type by using the -t argument.");
                return;
            }

            if (!arguments.QueryFile.Exists)
            {
                Console.WriteLine("No file to process was specified. Specify a file by using the -f argument.");
                return;
            }

            operation = arguments.Operation.Values.First();
            type = arguments.QueryType.Values.First();
            file = arguments.QueryFile.Values.First();

            if (operation != "e" && operation != "c")
            {
                Console.WriteLine($"The operation mode '{operation}' is not valid. Use -h to see a list of valid operation modes.");
                return;
            }

            if (type != "xq32" && type != "xseq")
            {
                Console.WriteLine($"The query type '{type}' is not valid. Use -h to see a list of valid query types.");
                return;
            }

            if (!File.Exists(file))
            {
                Console.WriteLine($"File '{Path.GetFullPath(file)}' was not found.");
                return;
            }

            #endregion

            switch (operation)
            {
                case "e":
                    ExtractFile(type, file);
                    break;

                case "c":
                    CreateFile(type, file);
                    break;
            }
        }

        private static void ExtractFile(string type, string file)
        {
            IParser parser;
            switch (type)
            {
                case "xq32":
                    parser = new Xq32Parser();
                    break;

                case "xseq":
                    parser = new XseqParser();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown type {type}.");
            }

            var outputFile = file + ".txt";
            var inputStream = File.OpenRead(file);
            var outputStream = File.Create(outputFile);

            parser.Decompile(inputStream, outputStream);

            inputStream.Close();
            outputStream.Close();
        }

        private static void CreateFile(string type, string file)
        {
            IParser parser;
            switch (type)
            {
                case "xq32":
                    parser = new Xq32Parser();
                    break;

                case "xseq":
                    parser = new XseqParser();
                    break;

                default:
                    throw new InvalidOperationException($"Unknown type {type}.");
            }

            var outputFile = file + ".xq";
            var inputStream = File.OpenRead(file);
            var outputStream = File.Create(outputFile);

            parser.Compile(inputStream, outputStream);

            inputStream.Close();
            outputStream.Close();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Following commands exist:");
            Console.WriteLine("  -h, --help\t\tShows this help message.");
            Console.WriteLine("  -o, --operation\tThe operation to take on the file");
            Console.WriteLine("    Valid operations are: e for extraction, c for creation");
            Console.WriteLine("  -t, --type\t\tThe type of file given");
            Console.WriteLine("    Valid types are: xq32, xseq");
            Console.WriteLine("  -f, --file\t\tThe file to process");
            Console.WriteLine();
            Console.WriteLine("Example usage:");
            Console.WriteLine($"\tExtract a xq32 query to human readable text: {Assembly.GetExecutingAssembly().Location} -o e -t xq32 -f [file]");
            Console.WriteLine($"\tCreate a xq32 query from human readable text: {Assembly.GetExecutingAssembly().Location} -o c -t xq32 -f [file]");
        }
    }
}
