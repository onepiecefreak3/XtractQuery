using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XtractQuery.IO;
using System.IO;
using XtractQuery.Compression;
using XtractQuery.Hash;
using System.Windows.Forms;
using XtractQuery.Parser;

namespace XtractQuery
{
    class Program
    {
        static void PrintHelp()
        {
            Console.WriteLine("This program converts an xq used in 3DS Professor Layton games from Level5 to a more human readable text file and vice versa.");
            Console.WriteLine($"\nUsage:\n{Path.GetFileName(Application.ExecutablePath)} <mode> [filepath] [donor XQ for table1]");
            Console.WriteLine($"\nSupported modes:\n" +
                $"\t-h\tShows this help\n" +
                $"\t-e\tExtracts a given xq\n" +
                $"\t-c\tCreate a xq from a given txt; donor for table1 only used here");
        }

        static void Main(string[] args)
        {
            #region Arguments Handling
            if (args.Count() == 0 || args[0] == "-h")
            {
                PrintHelp();
                return;
            }

            if (args.Count() < 2)
            {
                Console.WriteLine("Not enough arguments.");
                return;
            }

            if (args[0] != "-e" && args[0] != "-c" && args[0] != "-h")
            {
                Console.WriteLine("Mode not supported.");
                return;
            }

            if (args[0] == "-h")
            {
                PrintHelp();
                return;
            }
            #endregion

            string mode = args[0];
            string file = args[1];
            string donor = (args.Count() >= 3) ? args[2] : String.Empty;
            if (!File.Exists(file))
            {
                Console.WriteLine($"{file} not found.");
                return;
            }

            if (mode == "-e")
            {
                ExtractXQ(file);
            }
            else if (mode == "-c")
            {
                CreateXQ(file, donor);
            }

        }

        static void CreateXQ(string file, string donor)
        {
            using (var tp = new TXTParser(file, donor))
            {
                while (tp.ReadNextCodeBlock())
                {
                    while (tp.ReadNextCode())
                    {
                        ;
                    }
                }
                File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)) + ".xq2", tp.GetXQData());
            }
        }

        static void ExtractXQ(string file)
        {
            using (var sw = new StringWriter())
            using (var xqReader = new XQParser(file))
            {
                while (xqReader.ReadNextCodeBlock())
                {
                    sw.Write(xqReader.GetCodeBlockLine());
                    while (xqReader.ReadNextCode())
                    {
                        sw.Write(xqReader.GetCodeLine());
                    }
                }

                File.WriteAllText(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)) + ".txt", sw.ToString());
            }
        }
    }
}
