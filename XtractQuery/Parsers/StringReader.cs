using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Kryptography.Hash.Crc;
using XtractQuery.Interfaces;

namespace XtractQuery.Parsers
{
    class StringReader : IStringReader
    {
        private readonly Encoding _sjis;

        private BinaryReaderX _stringBr;
        private IDictionary<uint, string> _hashMap;

        public StringReader(Stream input)
        {
            _stringBr = new BinaryReaderX(input);
            _sjis = Encoding.GetEncoding("SJIS");

            HashStrings();
        }

        public string Read(long offset)
        {
            _stringBr.BaseStream.Position = offset;
            return _stringBr.ReadCStringSJIS();
        }

        public string GetByHash(uint hash)
        {
            if (!_hashMap.ContainsKey(hash))
                return null;

            return _hashMap[hash];
        }

        public void Dispose()
        {
            _stringBr.Dispose();
        }

        private void HashStrings()
        {
            _hashMap = new Dictionary<uint, string>();
            var crc32 = Crc32.Create(Crc32Formula.Normal);

            _stringBr.BaseStream.Position = 0;
            while (_stringBr.BaseStream.Position < _stringBr.BaseStream.Length)
            {
                var value = _stringBr.ReadCStringSJIS();
                _hashMap.Add(GetHash(crc32, value), value);
            }
        }

        private uint GetHash(Crc32 crc32, string value)
        {
            var data = crc32.Compute(_sjis.GetBytes(value));
            return BinaryPrimitives.ReadUInt32BigEndian(data);
        }
    }
}
