using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Komponent.IO;
using Kryptography.Hash.Crc;
using XtractQuery.Interfaces;

namespace XtractQuery.Parsers
{
    class StringWriter : IStringWriter
    {
        private readonly Encoding _sjis;

        private BinaryWriterX _streamWriter;
        private Crc32 _crc32;
        private Crc16 _crc16;

        private IDictionary<string, (uint, ushort, long)> _stringMap;

        public StringWriter(Stream stringStream)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _sjis = Encoding.GetEncoding("SJIS");

            _streamWriter = new BinaryWriterX(stringStream);
            _crc32 = Crc32.Create(Crc32Formula.Normal);
            _crc16 = Crc16.Create(Crc16Formula.X25);

            _stringMap = new Dictionary<string, (uint, ushort, long)>();
        }

        public long Write(string value)
        {
            if (_stringMap.ContainsKey(value))
                return _stringMap[value].Item3;

            var position = _streamWriter.BaseStream.Position;
            _streamWriter.WriteString(value, _sjis, false);

            _stringMap[value] = (CreateCrc32(value), CreateCrc16(value), position);

            return position;
        }

        public uint GetCrc32(string value)
        {
            if (_stringMap.ContainsKey(value))
                return _stringMap[value].Item1;

            var hash = CreateCrc32(value);
            return hash;
        }

        public ushort GetCrc16(string value)
        {
            if (_stringMap.ContainsKey(value))
                return _stringMap[value].Item2;

            var hash = CreateCrc16(value);
            return hash;
        }

        private uint CreateCrc32(string value)
        {
            var computed = _crc32.Compute(_sjis.GetBytes(value));
            return BinaryPrimitives.ReadUInt32BigEndian(computed);
        }

        private ushort CreateCrc16(string value)
        {
            var computed = _crc16.Compute(_sjis.GetBytes(value));
            return BinaryPrimitives.ReadUInt16BigEndian(computed);
        }
    }
}
