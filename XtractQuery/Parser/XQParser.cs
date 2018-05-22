using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using XtractQuery.IO;
using XtractQuery.Compression;

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

        }

        public bool ReadNextCodeBlock()
        {
            if (_currentCodeBlock + 1 >= codeBlocks.Count)
                return false;

            _currentCodeBlock++;
            return true;
        }

        public string GetCodeBlockName()
        {
            if (_currentCodeBlock + 1 >= codeBlocks.Count)
                return String.Empty;

            strings.BaseStream.Position = codeBlocks[_currentCodeBlock].nameOffset;
            return strings.ReadCStringSJIS();
        }

        public CodeBlock GetCodeBlockInfo()
        {
            if (_currentCodeBlock + 1 >= codeBlocks.Count)
                return null;

            return codeBlocks[_currentCodeBlock];
        }

        public bool ReadNextCode()
        {
            if (_currentCodeBlock < 0 || _currentCodeBlock + 1 >= codeBlocks.Count || _currentCode + 1 >= codeBlocks[_currentCodeBlock].t2EndOffset - codeBlocks[_currentCodeBlock].t2Offset)
                return false;

            _currentCode++;
            return true;
        }

        public void Dispose()
        {
            _stream.Close();
            strings.Close();
        }
    }
}
