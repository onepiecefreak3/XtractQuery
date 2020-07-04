using System;
using System.IO;
using System.Linq;
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
                Console.WriteLine("Specify a valid operation");
                return;
            }

            if (!arguments.QueryType.Exists)
            {
                Console.WriteLine("Specify a valid type");
                return;
            }

            if (!arguments.QueryFile.Exists)
            {
                Console.WriteLine("Specify a file");
                return;
            }

            operation = arguments.Operation.Values.First();
            type = arguments.QueryType.Values.First();
            file = arguments.QueryFile.Values.First();

            if (operation != "e" && operation != "c")
            {
                Console.WriteLine("Specify a valid operation");
                return;
            }

            if (type != "xq32" && type != "xseq")
            {
                Console.WriteLine("Specifiy a valid type");
                return;
            }

            if (!File.Exists(file))
            {
                Console.WriteLine($"File {file} does not exist");
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
        }
    }
}
