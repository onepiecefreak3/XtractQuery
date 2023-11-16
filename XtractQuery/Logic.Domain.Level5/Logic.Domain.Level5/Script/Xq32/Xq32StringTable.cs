using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Logic.Domain.Kuriimu2.KomponentAdapter.Contract;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;
using Logic.Domain.Level5.Cryptography.InternalContract;
using Logic.Domain.Level5.Script.Xq32.InternalContract;

namespace Logic.Domain.Level5.Script.Xq32
{
    internal class Xq32StringTable : IXq32StringTable
    {
        private readonly Stream _baseStream;
        private readonly IBinaryReaderX _reader;
        private readonly IBinaryWriterX? _writer;
        private readonly IChecksum<uint> _checksum;

        private readonly Encoding _sjis;
        private readonly IDictionary<uint, string> _hashLookup;

        private long _streamPosition;

        public Xq32StringTable(Stream stream, IBinaryFactory binaryFactory, IChecksumFactory checksumFactory)
        {
            _baseStream = stream;
            _reader = binaryFactory.CreateReader(_baseStream);
            _checksum = checksumFactory.CreateCrc32();

            _sjis = Encoding.GetEncoding("SJIS");
            _hashLookup = new Dictionary<uint, string>();

            _streamPosition = _baseStream.Position;

            InitializeHashLookup();
        }

        public Xq32StringTable(IBinaryFactory binaryFactory, IChecksumFactory checksumFactory)
        {
            _baseStream = new MemoryStream();
            _reader = binaryFactory.CreateReader(_baseStream);
            _writer = binaryFactory.CreateWriter(_baseStream);
            _checksum = checksumFactory.CreateCrc32();

            _sjis = Encoding.GetEncoding("SJIS");
            _hashLookup = new Dictionary<uint, string>();

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
            _hashLookup[ComputeHash(value)] = value;

            long valuePos = _streamPosition;
            _streamPosition = _baseStream.Position;

            _baseStream.Position = bkPos;
            return (int)valuePos;
        }

        public string GetByHash(uint hash)
        {
            if (_hashLookup.TryGetValue(hash, out string? value))
                return value;

            return string.Empty;
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
                uint checksum = _checksum.ComputeValue(value);

                _hashLookup[checksum] = value;
            }
        }
    }
}
