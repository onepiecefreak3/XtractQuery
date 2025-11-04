using System.Text;
using Komponent.IO;
using Kryptography.Checksum;
using Logic.Domain.Level5.InternalContract.Checksum;
using Logic.Domain.Level5.InternalContract.Script.Xseq;

namespace Logic.Domain.Level5.Script.Xseq;

internal class XseqStringTable : IXseqStringTable
{
    private static readonly Encoding SjisEncoding = Encoding.GetEncoding("Shift-JIS");

    private readonly Stream _baseStream;
    private readonly BinaryReaderX _reader;
    private readonly BinaryWriterX? _writer;
    private readonly Checksum<ushort> _checksum;

    private readonly Dictionary<ushort, List<string>> _hashLookup = [];

    private long _streamPosition;

    public XseqStringTable(Stream stream, IChecksumFactory checksumFactory)
    {
        _baseStream = stream;
        _reader = new BinaryReaderX(_baseStream, SjisEncoding);
        _checksum = checksumFactory.CreateCrc16();

        _streamPosition = _baseStream.Position;

        InitializeHashLookup();
    }

    public XseqStringTable(IChecksumFactory checksumFactory)
    {
        _baseStream = new MemoryStream();
        _reader = new BinaryReaderX(_baseStream, SjisEncoding);
        _writer = new BinaryWriterX(_baseStream);
        _checksum = checksumFactory.CreateCrc16();

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

        string value = _reader.ReadNullTerminatedString();

        _baseStream.Position = bkPos;
        return value;
    }

    public long Write(string value)
    {
        if (_writer == null)
            throw new InvalidOperationException("This instance does not support writing values.");

        long bkPos = _baseStream.Position;
        _baseStream.Position = _streamPosition;

        _writer.WriteString(value, SjisEncoding);

        var hash = (ushort)ComputeHash(value);
        if (!_hashLookup.TryGetValue(hash, out List<string>? values))
            _hashLookup[hash] = values = [];

        values.Add(value);

        long valuePos = _streamPosition;
        _streamPosition = _baseStream.Position;

        _baseStream.Position = bkPos;
        return (int)valuePos;
    }

    public IList<string> GetByHash(uint hash)
    {
        if (_hashLookup.TryGetValue((ushort)hash, out List<string>? values))
            return values;

        return Array.Empty<string>();
    }

    public uint ComputeHash(string value)
    {
        return _checksum.ComputeValue(value, SjisEncoding);
    }

    private void InitializeHashLookup()
    {
        while (_reader.BaseStream.Position < _reader.BaseStream.Length)
        {
            string value = _reader.ReadNullTerminatedString();
            ushort checksum = _checksum.ComputeValue(value);

            if (!_hashLookup.TryGetValue(checksum, out List<string>? values))
                _hashLookup[checksum] = values = [];

            values.Add(value);
        }
    }
}