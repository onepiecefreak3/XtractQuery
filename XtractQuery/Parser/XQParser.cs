using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using XtractQuery.IO;
using XtractQuery.Compression;
using XtractQuery.Hash;

namespace XtractQuery.Parser
{
    public class XQParser : IDisposable
    {
        BinaryReaderY _stream;

        Header header;
        List<CodeBlock> codeBlocks;
        List<AdditionalCodeBinding> bindings;
        List<FuncStruct> codes;
        List<VarStruct> variables;
        BinaryReaderY strings;

        int _currentCodeBlock = -1;
        int _currentCode = -1;

        public XQParser(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"File {file} was not found.");

            if (new BinaryReaderY(File.OpenRead(file)).ReadString(4) != "XQ32")
                throw new InvalidDataException("This is no xq file.");

            _stream = new BinaryReaderY(File.OpenRead(file));

            ParseTables();
            SanityCheck();
        }

        private void ParseTables()
        {
            header = _stream.ReadStruct<Header>();

            _stream.BaseStream.Position = header.table0Offset << 2;
            codeBlocks = new BinaryReaderY(new MemoryStream(Level5.Decompress(_stream.BaseStream))).ReadMultiple<CodeBlock>(header.table0EntryCount).OrderBy(t0 => t0.t2Offset).ToList();
            _stream.BaseStream.Position = header.table1Offset << 2;
            bindings = new BinaryReaderY(new MemoryStream(Level5.Decompress(_stream.BaseStream))).ReadMultiple<AdditionalCodeBinding>(header.table1EntryCount);
            _stream.BaseStream.Position = header.table2Offset << 2;
            codes = new BinaryReaderY(new MemoryStream(Level5.Decompress(_stream.BaseStream))).ReadMultiple<FuncStruct>(header.table2EntryCount);
            _stream.BaseStream.Position = header.table3Offset << 2;
            variables = new BinaryReaderY(new MemoryStream(Level5.Decompress(_stream.BaseStream))).ReadMultiple<VarStruct>(header.table3EntryCount);
            _stream.BaseStream.Position = header.table4Offset << 2;
            strings = new BinaryReaderY(new MemoryStream(Level5.Decompress(_stream.BaseStream)));
        }

        private void SanityCheck()
        {
            foreach (var t0 in codeBlocks)
            {
                strings.BaseStream.Position = t0.nameOffset;
                var name = strings.ReadCStringSJIS();
                if (Crc32.Create(Encoding.GetEncoding("SJIS").GetBytes(name)) != t0.hash)
                    throw new Exception($"Table0: {name} hasn't produced hash 0x{t0.hash:x8}");
            }

            //Table 1 Integrity check
            foreach (var t1 in bindings)
            {
                strings.BaseStream.Position = t1.nameOffset;
                var name = strings.ReadCStringSJIS();
                if (Crc32.Create(Encoding.GetEncoding("SJIS").GetBytes(name)) != t1.hash)
                    throw new Exception($"Table1: {name} hasn't produced hash 0x{t1.hash:x8}");
            }
        }

        public bool ReadNextCodeBlock()
        {
            if (_currentCodeBlock + 1 >= codeBlocks.Count)
                return false;

            _currentCodeBlock++;
            _currentCode = codeBlocks[_currentCodeBlock].t2Offset - 1;
            return true;
        }

        public string GetCodeBlockName()
        {
            if (_currentCodeBlock >= codeBlocks.Count || _currentCodeBlock < 0)
                return String.Empty;

            strings.BaseStream.Position = codeBlocks[_currentCodeBlock].nameOffset;
            return strings.ReadCStringSJIS();
        }

        public string GetCodeBlockLine()
        {
            return GetCodeBlockName() + ":\r\n";
        }

        public CodeBlock GetCodeBlockInfo()
        {
            if (_currentCodeBlock + 1 >= codeBlocks.Count)
                return null;

            return codeBlocks[_currentCodeBlock];
        }

        public CodeBlock GetCodeBlockInfo(string name)
        {
            foreach (var cb in codeBlocks)
            {
                strings.BaseStream.Position = cb.nameOffset;
                var cn = strings.ReadCStringSJIS();
                if (cn == name)
                    return cb;
            }

            return null;
        }

        public List<AdditionalCodeBinding> GetTable1() => bindings;

        public IEnumerable<string> GetTable1Names()
        {
            foreach (var t1e in bindings)
            {
                strings.BaseStream.Position = t1e.nameOffset;
                yield return strings.ReadCStringSJIS();
            }
        }

        public bool ReadNextCode()
        {
            if (_currentCodeBlock < 0 || _currentCodeBlock + 1 >= codeBlocks.Count || _currentCode + 1 >= codeBlocks[_currentCodeBlock].t2EndOffset - codeBlocks[_currentCodeBlock].t2Offset)
                return false;

            _currentCode++;
            return true;
        }

        public string GetCodeLine()
        {
            if (_currentCodeBlock < 0 || _currentCodeBlock + 1 >= codeBlocks.Count || _currentCode + 1 >= codeBlocks[_currentCodeBlock].t2EndOffset - codeBlocks[_currentCodeBlock].t2Offset)
                return String.Empty;

            string result = "";

            result += "\tsub" + codes[_currentCode].subType;
            result += "<" + codes[_currentCode].unk1 + ">" + "(";

            for (int i = codes[_currentCode].varOffset; i < codes[_currentCode].varOffset + codes[_currentCode].varCount; i++)
            {
                switch (variables[i].type >> 1)
                {
                    case 0:
                        result += variables[i].value;
                        break;
                    case 1:
                        if (codes[_currentCode].subType == 0x14 && i == codes[_currentCode].varOffset && Listings.OpCodes.ContainsKey(variables[i].value))
                            result += "\"" + Listings.OpCodes[variables[i].value] + "\"";
                        else
                            result += variables[i].value;
                        break;
                    case 2:
                        result += variables[i].value;
                        break;
                    case 0xc:
                        strings.BaseStream.Position = variables[i].value;
                        result += "\"" + strings.ReadCStringSJIS() + "\"";
                        break;
                }
                result += $"<{variables[i].type >> 1}:{variables[i].type & 1}>";

                if (i != codes[_currentCode].varOffset + codes[_currentCode].varCount - 1)
                    result += ", ";
            }

            result += ");\r\n";

            return result;
        }

        public void Dispose()
        {
            _stream.Close();
            strings.Close();
        }
    }
}
