﻿using System.Text;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;
using Logic.Domain.Level5.Cryptography.InternalContract;
using Logic.Domain.Level5.Script.Xseq.InternalContract;

namespace Logic.Domain.Level5.Script.Xseq
{
    internal class XseqStringTable : IXseqStringTable
    {
        private readonly Stream _baseStream;
        private readonly IBinaryReaderX _reader;
        private readonly IBinaryWriterX? _writer;
        private readonly IChecksum<ushort> _checksum;

        private readonly Encoding _sjis;
        private readonly IDictionary<ushort, IList<string>> _hashLookup;

        private long _streamPosition;

        public XseqStringTable(Stream stream, IBinaryFactory binaryFactory, IChecksumFactory checksumFactory)
        {
            _baseStream = stream;
            _reader = binaryFactory.CreateReader(_baseStream);
            _checksum = checksumFactory.CreateCrc16();

            _sjis = Encoding.GetEncoding("SJIS");
            _hashLookup = new Dictionary<ushort, IList<string>>();

            _streamPosition = _baseStream.Position;

            InitializeHashLookup();
        }

        public XseqStringTable(IBinaryFactory binaryFactory, IChecksumFactory checksumFactory)
        {
            _baseStream = new MemoryStream();
            _reader = binaryFactory.CreateReader(_baseStream);
            _writer = binaryFactory.CreateWriter(_baseStream);
            _checksum = checksumFactory.CreateCrc16();

            _sjis = Encoding.GetEncoding("SJIS");
            _hashLookup = new Dictionary<ushort, IList<string>>();

            _streamPosition = 0;
        }

        public Stream GetStream()
        {
            return _baseStream;
        }

        public string Read(long offset)
        {
            long bkPos = _baseStream.Position;
            _baseStream.Position = offset;

            string value = _reader.ReadCStringSJIS();

            _baseStream.Position = bkPos;
            return value;
        }

        public long Write(string value)
        {
            if (_writer == null)
                throw new InvalidOperationException("This instance does not support writing values.");

            long bkPos = _baseStream.Position;
            _baseStream.Position = _streamPosition;

            _writer.WriteString(value, _sjis, false);

            var hash = (ushort)ComputeHash(value);
            if (!_hashLookup.TryGetValue(hash, out IList<string>? values))
                _hashLookup[hash] = values = new List<string>();

            values.Add(value);

            long valuePos = _streamPosition;
            _streamPosition = _baseStream.Position;

            _baseStream.Position = bkPos;
            return (int)valuePos;
        }

        public IList<string> GetByHash(uint hash)
        {
            if (_hashLookup.TryGetValue((ushort)hash, out IList<string>? values))
                return values;

            return Array.Empty<string>();
        }

        public uint ComputeHash(string value)
        {
            return _checksum.ComputeValue(value, _sjis);
        }

        private void InitializeHashLookup()
        {
            while (_reader.BaseStream.Position < _reader.BaseStream.Length)
            {
                string value = _reader.ReadCStringSJIS();
                ushort checksum = _checksum.ComputeValue(value);

                if (!_hashLookup.TryGetValue(checksum, out IList<string>? values))
                    _hashLookup[checksum] = values = new List<string>();

                values.Add(value);
            }
        }
    }
}
