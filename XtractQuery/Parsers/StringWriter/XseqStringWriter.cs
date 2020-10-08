using System.Buffers.Binary;
using System.IO;
using Kryptography.Hash.Crc;

namespace XtractQuery.Parsers.StringWriter
{
    class XseqStringWriter : BaseStringWriter
    {
        private readonly Crc16 _crc16;

        public XseqStringWriter(Stream stringStream) : base(stringStream)
        {
            _crc16 = Crc16.Create(Crc16Formula.X25);
        }

        protected override uint CreateHash(string value)
        {
            var computed = _crc16.Compute(SjisEncoding.GetBytes(value));
            return BinaryPrimitives.ReadUInt32BigEndian(computed);
        }
    }
}
