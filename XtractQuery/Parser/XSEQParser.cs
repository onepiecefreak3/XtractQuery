using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using XtractQuery.IO;
using XtractQuery.Compression;

namespace XtractQuery.Parser
{
    public class XSEQParser : IDisposable
    {
        BinaryReaderY _stream;

        Header header;
        List<XseqCodeBlock> codeBlocks;
        List<XseqAdditionalCodeBinding> bindings;
        List<XseqFuncStruct> codes;
        List<XseqVarStruct> variables;
        BinaryReaderY strings;

        int _currentCodeBlock = -1;
        int _currentCode = -1;

        public XSEQParser(string file)
        {
            if (!File.Exists(file))
                throw new Exception($"File {file} was not found.");

            var magic = new BinaryReaderY(File.OpenRead(file)).ReadString(4);
            if (magic != "XSEQ")
                throw new InvalidDataException("This is no xq file.");

            _stream = new BinaryReaderY(File.OpenRead(file));

            ParseTables();
        }

        private void ParseTables()
        {
            header = _stream.ReadStruct<Header>();

            _stream.BaseStream.Position = header.table0Offset << 2;
            codeBlocks = new BinaryReaderY(new MemoryStream(Level5.Decompress(_stream.BaseStream))).ReadMultiple<XseqCodeBlock>(header.table0EntryCount).OrderBy(t0 => t0.instructionOffset).ToList();
            _stream.BaseStream.Position = header.table1Offset << 2;
            bindings = new BinaryReaderY(new MemoryStream(Level5.Decompress(_stream.BaseStream))).ReadMultiple<XseqAdditionalCodeBinding>(header.table1EntryCount);
            _stream.BaseStream.Position = header.table2Offset << 2;
            codes = new BinaryReaderY(new MemoryStream(Level5.Decompress(_stream.BaseStream))).ReadMultiple<XseqFuncStruct>(header.table2EntryCount);
            _stream.BaseStream.Position = header.table3Offset << 2;
            variables = new BinaryReaderY(new MemoryStream(Level5.Decompress(_stream.BaseStream))).ReadMultiple<XseqVarStruct>(header.table3EntryCount);
            _stream.BaseStream.Position = header.table4Offset << 2;
            strings = new BinaryReaderY(new MemoryStream(Level5.Decompress(_stream.BaseStream)));
        }

        public bool ReadNextCodeBlock()
        {
            if (_currentCodeBlock + 1 >= codeBlocks.Count)
                return false;

            _currentCodeBlock++;
            _currentCode = codeBlocks[_currentCodeBlock].instructionOffset - 1;
            return true;
        }

        public string GetCodeBlockName()
        {
            if (_currentCodeBlock >= codeBlocks.Count || _currentCodeBlock < 0)
                return string.Empty;

            strings.BaseStream.Position = codeBlocks[_currentCodeBlock].functionNameOffset;
            return strings.ReadCStringSJIS();
        }

        public string GetCodeBlockLine()
        {
            return GetCodeBlockName() + ":\r\n";
        }

        public XseqCodeBlock GetCodeBlockInfo()
        {
            if (_currentCodeBlock + 1 >= codeBlocks.Count)
                return null;

            return codeBlocks[_currentCodeBlock];
        }

        public XseqCodeBlock GetCodeBlockInfo(string name)
        {
            foreach (var cb in codeBlocks)
            {
                strings.BaseStream.Position = cb.functionNameOffset;
                var cn = strings.ReadCStringSJIS();
                if (cn == name)
                    return cb;
            }

            return null;
        }

        public List<XseqAdditionalCodeBinding> GetTable1() => bindings;

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
            if (_currentCodeBlock < 0 || _currentCodeBlock >= codeBlocks.Count || _currentCode + 1 >= codeBlocks[_currentCodeBlock].instructionEndOffset)
                return false;

            _currentCode++;
            return true;
        }

        public string GetCodeLine()
        {
            if (_currentCodeBlock < 0 || _currentCodeBlock >= codeBlocks.Count || _currentCode >= codeBlocks[_currentCodeBlock].instructionEndOffset)
                return string.Empty;

            string result = "";

            result += "\tsub" + codes[_currentCode].subroutine;
            result += "<" + codes[_currentCode].unk1 + ">" + "(";

            for (int i = codes[_currentCode].varOffset; i < codes[_currentCode].varOffset + codes[_currentCode].varCount; i++)
            {
                switch (variables[i].type >> 1)
                {
                    case 0:
                        result += variables[i].value;
                        break;
                    case 1:
                        if (codes[_currentCode].subroutine == 0x14 && i == codes[_currentCode].varOffset && Listings.OpCodes.ContainsKey(variables[i].value))
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
