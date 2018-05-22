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
            Console.WriteLine($"\nUsage:\n{Path.GetFileName(Application.ExecutablePath)} <mode> [filepath]");
            Console.WriteLine($"\nSupported modes:\n" +
                $"\t-h\tShows this help\n" +
                $"\t-e\tExtracts a given xq\n" +
                $"\t-c\tCreate a xq from a given txt");
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
                CreateXQ(file);
            }

        }

        static void CreateXQ(string file)
        {
            if (Path.GetExtension(file) != ".txt")
            {
                Console.WriteLine("No supported text file.");
                return;
            }

            var lines = File.ReadAllLines(file);
            #region Syntax check
            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].Replace("\t", "") != String.Empty)
                    if (lines[i].Contains('('))
                    {
                        if (lines[i].Substring(lines[i].Count() - 2, 2) != ");")
                            throw new Exception($"Sub has syntax errors in line {i}.");
                    }
                    else
                    {
                        if (lines[i].Last() != ':')
                            throw new Exception($"Commandblock label has syntax errors in line {i}.");
                    }
            }
            #endregion

            var sjis = Encoding.GetEncoding("SJIS");

            var table0 = new List<T0Entry>();
            var funcs = new List<FuncStruct>();
            var vars = new List<VarStruct>();

            var stringDic = new Dictionary<string, int>();
            var stringOffset = 0;
            var computedSubs = 0;

            #region Parse txt to lists and structs
            foreach (var line in lines)
            {
                //check if line is commandblock indicator
                if (line.Last() == ':')
                {
                    if (computedSubs > 0 && table0.Count > 0)
                        table0.Last().t2EndOffset = (short)(funcs.Count);

                    var name = line.Split(':')[0];
                    var intStringOffset = stringOffset;
                    if (stringDic.ContainsKey(name))
                        intStringOffset = stringDic[name];
                    else
                    {
                        stringDic.Add(name, stringOffset);
                        stringOffset += sjis.GetByteCount(name) + 1;
                    }

                    table0.Add(new T0Entry
                    {
                        nameOffset = intStringOffset,
                        hash = Crc32.Create(name),
                        t2Offset = (short)(funcs.Count),
                        t2EndOffset = (short)(funcs.Count)
                    });
                }
                //Else it's a subLine
                else
                {
                    if (line.Replace("\t", "") == String.Empty)
                        continue;

                    var workingLine = line.Replace("\t", "");
                    var subSplit = workingLine.Split(new string[] { ">(" }, StringSplitOptions.None);

                    var subType = Convert.ToInt16(subSplit[0].Split('<')[0].Replace("sub", ""));
                    var unk1 = Convert.ToInt16(subSplit[0].Split('<')[1].Split('>')[0]);
                    var varOffset = (short)vars.Count;

                    var args = subSplit[1].Split(new string[] { ");" }, StringSplitOptions.None)[0].Split(new string[] { ", " }, StringSplitOptions.None);

                    foreach (var arg in args)
                    {
                        if (arg == String.Empty)
                            continue;

                        var type = arg.Split('>')[0].Split('<')[1];

                        var valuePart = arg.Split('<')[0];
                        uint value = 0;
                        if (valuePart.Contains('"'))
                        {
                            value = (uint)stringOffset;
                            if (stringDic.ContainsKey(valuePart.Replace("\"", "")))
                                value = (uint)stringDic[valuePart.Replace("\"", "")];
                            else
                            {
                                stringDic.Add(valuePart.Replace("\"", ""), stringOffset);
                                stringOffset += sjis.GetByteCount(valuePart.Replace("\"", "")) + 1;
                            }
                        }
                        else
                            value = Convert.ToUInt32(valuePart);

                        vars.Add(new VarStruct
                        {
                            type = GetType(type),
                            value = value,
                        });
                    }

                    funcs.Add(new FuncStruct
                    {
                        varOffset = varOffset,
                        varCount = (short)(vars.Count - varOffset),
                        unk1 = unk1,
                        subType = subType
                    });

                    computedSubs++;
                }
            }
            #endregion

            #region Write to file
            var header = new Header
            {
                table0EntryCount = (short)table0.Count,
                table2EntryCount = (short)funcs.Count,
                table3EntryCount = (short)vars.Count
            };
            table0 = table0.OrderBy(t0 => t0.hash).ToList();

            using (var bw = new BinaryWriterY(File.Create(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)) + ".xq")))
            {
                //Table0
                bw.BaseStream.Position = 0x20;
                bw.WriteMultipleCompressed(table0, Level5.Method.LZ10);
                bw.WriteAlignment(4);

                //Table1 - Ignore for now
                header.table1Offset = (short)(bw.BaseStream.Position >> 2);
                bw.Write(0);

                //Functions
                header.table2Offset = (short)(bw.BaseStream.Position >> 2);
                bw.WriteMultipleCompressed(funcs, Level5.Method.LZ10);
                bw.WriteAlignment(4);

                //Variables
                header.table3Offset = (short)(bw.BaseStream.Position >> 2);
                bw.WriteMultipleCompressed(vars, Level5.Method.LZ10);
                bw.WriteAlignment(4);

                //Strings
                header.table4Offset = (short)(bw.BaseStream.Position >> 2);
                bw.WriteStringsCompressed(stringDic.Select(sd => sd.Key), Level5.Method.LZ10, sjis);
                bw.WriteAlignment(4);

                //Header
                bw.BaseStream.Position = 0;
                bw.WriteStruct(header);
            }
            #endregion
        }

        static int GetType(string input) => (Convert.ToInt32(input.Split(':')[0]) << 1) | (Convert.ToInt32(input.Split(':')[1]) & 1);

        static void ExtractXQ(string file)
        {
            using (var xqReader = new XQParser(file))
            {
                while (xqReader.ReadNextCodeBlock())
                {
                    xqReader.GetCodeBlockName();
                    while (xqReader.ReadNextCode())
                    {
                        xqReader.GetCodeSub();
                        xqReader.GetCodeArguments();
                    }
                }
            }

            if (new BinaryReaderY(File.OpenRead(file)).ReadString(4) != "XQ32")
                throw new InvalidDataException("This is no xq file.");

            var result = "";
            using (var xq = new BinaryReaderY(File.OpenRead(file)))
            {
                //Header
                var header = xq.ReadStruct<Header>();

                //Decompressed tables
                xq.BaseStream.Position = header.table0Offset << 2;
                var table0 = new BinaryReaderY(new MemoryStream(Level5.Decompress(xq.BaseStream))).ReadMultiple<T0Entry>(header.table0EntryCount).OrderBy(t0 => t0.t2Offset);
                xq.BaseStream.Position = header.table1Offset << 2;
                var table1 = new BinaryReaderY(new MemoryStream(Level5.Decompress(xq.BaseStream))).ReadMultiple<T1Entry>(header.table1EntryCount);
                xq.BaseStream.Position = header.table2Offset << 2;
                var funcs = new BinaryReaderY(new MemoryStream(Level5.Decompress(xq.BaseStream))).ReadMultiple<FuncStruct>(header.table2EntryCount);
                xq.BaseStream.Position = header.table3Offset << 2;
                var variables = new BinaryReaderY(new MemoryStream(Level5.Decompress(xq.BaseStream))).ReadMultiple<VarStruct>(header.table3EntryCount);
                xq.BaseStream.Position = header.table4Offset << 2;
                var strings = Level5.Decompress(xq.BaseStream);

                using (var stringTable = new BinaryReaderY(new MemoryStream(strings)))
                {
                    #region Integrity checks
                    //Table 0 Integrity check
                    foreach (var t0 in table0)
                    {
                        stringTable.BaseStream.Position = t0.nameOffset;
                        var name = stringTable.ReadCStringSJIS();
                        if (Crc32.Create(Encoding.GetEncoding("SJIS").GetBytes(name)) != t0.hash)
                            throw new Exception($"Table0: {name} hasn't produced hash 0x{t0.hash:x8}");
                    }

                    //Table 1 Integrity check
                    foreach (var t1 in table1)
                    {
                        stringTable.BaseStream.Position = t1.nameOffset;
                        var name = stringTable.ReadCStringSJIS();
                        if (Crc32.Create(Encoding.GetEncoding("SJIS").GetBytes(name)) != t1.hash)
                            throw new Exception($"Table1: {name} hasn't produced hash 0x{t1.hash:x8}");
                    }
                    #endregion

                    foreach (var commandblock in table0)
                    {
                        stringTable.BaseStream.Position = commandblock.nameOffset;
                        var commandBlockName = stringTable.ReadCStringSJIS();
                        result += commandBlockName + ":\n";

                        for (int ind = commandblock.t2Offset; ind < commandblock.t2EndOffset; ind++)
                        {
                            result += "\tsub" + funcs[ind].subType;
                            result += "<" + funcs[ind].unk1 + ">" + "(";

                            for (int i = funcs[ind].varOffset; i < funcs[ind].varOffset + funcs[ind].varCount; i++)
                            {
                                switch (variables[i].type >> 1)
                                {
                                    case 0:
                                    case 1:
                                    case 2:
                                        result += variables[i].value;
                                        break;
                                    case 0xc:
                                        stringTable.BaseStream.Position = variables[i].value;
                                        result += "\"" + stringTable.ReadCStringSJIS() + "\"";
                                        break;
                                }
                                result += $"<{variables[i].type >> 1}:{variables[i].type & 1}>";

                                if (i != funcs[ind].varOffset + funcs[ind].varCount - 1)
                                    result += ", ";
                            }

                            result += ");\n";
                        }
                    }
                }
            }

            File.WriteAllText(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)) + ".txt", result);
        }


    }

    public static class Extensions
    {
        public static void WriteMultipleCompressed<T>(this BinaryWriterY bw, IEnumerable<T> list, Level5.Method comp)
        {
            if (list.Count() <= 0)
            {
                bw.Write(0);
                return;
            }

            var ms = new MemoryStream();
            using (var bwIntern = new BinaryWriterY(ms, true))
                foreach (var t in list)
                    bwIntern.WriteStruct(t);
            bw.Write(Level5.Compress(ms, comp));
        }

        public static void WriteStringsCompressed(this BinaryWriterY bw, IEnumerable<string> list, Level5.Method comp, Encoding enc)
        {
            var ms = new MemoryStream();
            using (var bwIntern = new BinaryWriterY(ms, true))
                foreach (var t in list)
                {
                    bwIntern.Write(enc.GetBytes(t));
                    bwIntern.Write((byte)0);
                }
            bw.Write(Level5.Compress(ms, comp));
        }
    }
}
