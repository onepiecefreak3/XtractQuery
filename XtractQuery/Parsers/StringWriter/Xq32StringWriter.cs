using System.Buffers.Binary;
using System.IO;
using Kryptography.Hash.Crc;

namespace XtractQuery.Parsers.StringWriter
{
    class Xq32StringWriter : BaseStringWriter
    {
        private readonly Crc32 _crc32;

        public Xq32StringWriter(Stream stringStream) : base(stringStream)
        {
            _crc32 = Crc32.Create(Crc32Formula.Normal);
        }

        protected override uint CreateHash(string value)
        {
            var computed = _crc32.Compute(SjisEncoding.GetBytes(value));
            return BinaryPrimitives.ReadUInt32BigEndian(computed);
        }
    }
}
