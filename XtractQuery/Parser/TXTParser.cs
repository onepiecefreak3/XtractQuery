using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using XtractQuery.IO;
using XtractQuery.Hash;
using XtractQuery.Compression;
using System.Text.RegularExpressions;

namespace XtractQuery.Parser
{
    public class TXTParser : IDisposable
    {
        StringReader sr;
        XQParser xq;
        Encoding sjis = Encoding.GetEncoding("SJIS");

        string _currentBlockLine = null;
        string _currentCodeLine = null;

        List<CodeBlock> codeBlocks = new List<CodeBlock>();
        List<FuncStruct> codes = new List<FuncStruct>();
        List<VarStruct> variables = new List<VarStruct>();
        Dictionary<int, string> strings = new Dictionary<int, string>();

        string _codeReadBuffer = null;

        public TXTParser(string file, string donor)
        {
            if (!File.Exists(file))
                throw new Exception($"File {file} was not found.");
            if (!File.Exists(donor))
                throw new Exception($"File {donor} was not found.");

            if (new BinaryReaderY(File.OpenRead(donor)).ReadString(4) != "XQ32")
                throw new InvalidDataException($"{donor} is no xq file.");

            xq = new XQParser(donor);
            sr = new StringReader(File.ReadAllText(file));
        }

        public bool ReadNextCodeBlock()
        {
            if (codeBlocks.Count > 0)
                codeBlocks.Last().t2EndOffset = (short)codes.Count;

            if (_codeReadBuffer != null)
            {
                if (_codeReadBuffer.Last() == ':')
                {
                    _currentBlockLine = _codeReadBuffer;
                    SetCodeBlock();
                    _codeReadBuffer = null;
                    return true;
                }
                _codeReadBuffer = null;
            }

            string line;
            while ((line = sr.ReadLine()) != null)
                if (line.Last() == ':')
                {
                    _currentBlockLine = line;
                    SetCodeBlock();
                    return true;
                }

            _currentBlockLine = null;
            return false;
        }

        private void SetCodeBlock()
        {
            if (_currentBlockLine != null)
            {
                var blockName = _currentBlockLine.Split(':')[0];

                int stringOffset = AddString(blockName);

                var matchingCodeBlock = xq.GetCodeBlockInfo(blockName);

                codeBlocks.Add(new CodeBlock
                {
                    nameOffset = stringOffset,
                    hash = Crc32.Create(blockName),
                    t2Offset = (short)codes.Count,
                    t1Offset = (matchingCodeBlock != null) ? matchingCodeBlock.t1Offset : (short)0,
                    t1EntryCount = (matchingCodeBlock != null) ? matchingCodeBlock.t1EntryCount : (short)0,
                    t1Offset2 = (matchingCodeBlock != null) ? matchingCodeBlock.t1Offset2 : (short)0,
                    t1Count2 = (matchingCodeBlock != null) ? matchingCodeBlock.t1Count2 : (short)0,
                    unk7 = (matchingCodeBlock != null) ? matchingCodeBlock.unk7 : 0
                });
            }
        }

        public bool ReadNextCode()
        {
            string line;
            while ((line = sr.ReadLine()) != null)
                if (line.Last() == ';' && line.Replace("\t", "") != String.Empty)
                {
                    _currentCodeLine = line.Replace("\t", "");
                    SetCode();
                    return true;
                }
                else
                {
                    break;
                }

            _codeReadBuffer = line;
            _currentCodeLine = null;
            return false;
        }

        private void SetCode()
        {
            if (_currentCodeLine != null)
            {
                var subPart = _currentCodeLine.Split(new string[] { "sub" }, StringSplitOptions.None)[1].Split('(')[0];
                var args = _currentCodeLine.Split(new string[] { ">(" }, StringSplitOptions.None)[1].Split(new string[] { ");" }, StringSplitOptions.None)[0].Replace(", ", ",").Split(',');
                if (args[0] == String.Empty)
                    args = new string[0];

                var func = new FuncStruct
                {
                    varOffset = (short)variables.Count,
                    varCount = (short)args.Count(),
                    unk1 = Convert.ToInt16(subPart.Split('<')[1].Split('>')[0]),
                    subType = Convert.ToInt16(subPart.Split('<')[0])
                };

                if (args.Count() > 0 && args[0] != String.Empty)
                    foreach (var arg in args)
                    {
                        var value = arg.Split('<')[0];
                        var unk = arg.Split('<')[1].Split('>')[0].Split(':');

                        VarStruct variable = new VarStruct
                        {
                            type = (Convert.ToInt32(unk[0]) << 1) | Convert.ToInt32(unk[1])
                        };

                        if (value.Contains("\""))
                        {
                            if (Convert.ToInt32(unk[0]) == 0xc)
                            {
                                int stringOffset = AddString(value.Split('"')[1]);
                                variable.value = (uint)stringOffset;
                            }
                            else if (Convert.ToInt32(unk[0]) == 0x1)
                            {
                                if (!Listings.OpCodes.ContainsValue(value.Split('"')[1]))
                                    throw new Exception($"{value.Split('"')[1]} can't be used as a hashed event name.");

                                variable.value = Listings.OpCodes.Where(c => c.Value == value.Split('"')[1]).First().Key;
                            }
                        }
                        else
                        {
                            variable.value = Convert.ToUInt32(value);
                        }

                        variables.Add(variable);
                    }

                codes.Add(func);
            }
        }

        public int AddString(string name)
        {
            int stringOffset;

            if (strings.ContainsValue(name))
                stringOffset = strings.Where(s => s.Value == name).First().Key;
            else
            {
                if (strings.Count <= 0)
                    stringOffset = 0;
                else
                    stringOffset = strings.Last().Key + sjis.GetByteCount(strings.Last().Value) + 1;
                strings.Add(stringOffset, name);
            }

            return stringOffset;
        }

        public byte[] GetXQData()
        {
            var header = new Header()
            {
                table0EntryCount = (short)codeBlocks.Count,
                table1EntryCount = (short)xq.GetTable1().Count,
                table2EntryCount = (short)codes.Count,
                table3EntryCount = (short)variables.Count
            };

            var table1 = xq.GetTable1();
            var t1s = xq.GetTable1Names().ToList();
            for (int i = 0; i < table1.Count; i++)
                table1[i].nameOffset = AddString(t1s[i]);

            var ms = new MemoryStream();
            using (var bw = new BinaryWriterY(ms, true))
            {
                bw.BaseStream.Position = header.table0Offset << 2;
                bw.WriteMultipleCompressed(codeBlocks, Level5.Method.LZ10);
                bw.WriteAlignment(4);

                header.table1Offset = (short)(bw.BaseStream.Position >> 2);
                bw.WriteMultipleCompressed(table1, Level5.Method.LZ10);
                bw.WriteAlignment(4);

                header.table2Offset = (short)(bw.BaseStream.Position >> 2);
                bw.WriteMultipleCompressed(codes, Level5.Method.LZ10);
                bw.WriteAlignment(4);

                header.table3Offset = (short)(bw.BaseStream.Position >> 2);
                bw.WriteMultipleCompressed(variables, Level5.Method.LZ10);
                bw.WriteAlignment(4);

                header.table4Offset = (short)(bw.BaseStream.Position >> 2);
                bw.WriteStringsCompressed(strings.Select(s => s.Value), Level5.Method.LZ10, sjis);
                bw.WriteAlignment(4);

                bw.BaseStream.Position = 0;
                bw.WriteStruct(header);
            }

            return ms.ToArray();
        }

        public void Dispose()
        {
            sr.Dispose();
            xq.Dispose();
        }
    }
}
