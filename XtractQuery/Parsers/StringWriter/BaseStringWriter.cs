using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Komponent.IO;
using XtractQuery.Interfaces;

namespace XtractQuery.Parsers.StringWriter
{
    abstract class BaseStringWriter : IStringWriter
    {
        protected Encoding SjisEncoding { get; }

        private BinaryWriterX _streamWriter;

        private IDictionary<string, (uint, long)> _stringMap;

        public BaseStringWriter(Stream stringStream)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SjisEncoding = Encoding.GetEncoding("SJIS");

            _streamWriter = new BinaryWriterX(stringStream);

            _stringMap = new Dictionary<string, (uint, long)>();
        }

        public long Write(string value)
        {
            // If string is already registered
            if (_stringMap.ContainsKey(value))
                return _stringMap[value].Item2;

            // If string is empty and the map already has at least one entry
            if (string.IsNullOrEmpty(value) && _stringMap.Count > 0)
            {
                var element = _stringMap.First(x => x.Value.Item2 == 0);
                return SjisEncoding.GetBytes(element.Key).Length;
            }

            // If string is already partially included in another
            if (!string.IsNullOrEmpty(value) && _stringMap.Keys.Any(x => x.EndsWith(value)))
            {
                var element = _stringMap.First(x => x.Key.EndsWith(value));
                var internalPosition = element.Value.Item2 + element.Key.LastIndexOf(value, StringComparison.InvariantCulture);
                _stringMap[value] = (CreateHash(value), internalPosition);

                return internalPosition;
            }

            var position = _streamWriter.BaseStream.Position;
            _streamWriter.WriteString(value, SjisEncoding, false);

            _stringMap[value] = (CreateHash(value), position);

            return position;
        }

        public uint GetHash(string value)
        {
            if (_stringMap.ContainsKey(value))
                return _stringMap[value].Item1;

            var hash = CreateHash(value);
            return hash;
        }

        protected abstract uint CreateHash(string value);
    }
}
