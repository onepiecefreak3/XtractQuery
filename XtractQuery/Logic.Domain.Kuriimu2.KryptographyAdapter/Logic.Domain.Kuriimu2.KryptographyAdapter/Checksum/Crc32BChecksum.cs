using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kryptography.Hash.Crc;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum
{
    internal class Crc32BChecksum : ICrc32BChecksum
    {
        private readonly Crc32 _crc;

        public Crc32BChecksum()
        {
            _crc = Crc32.Crc32B;
        }

        public byte[] Compute(string input)
        {
            return _crc.Compute(input);
        }

        public byte[] Compute(string input, Encoding enc)
        {
            return _crc.Compute(input, enc);
        }

        public byte[] Compute(Stream input)
        {
            return _crc.Compute(input);
        }

        public byte[] Compute(Span<byte> input)
        {
            return _crc.Compute(input);
        }

        public uint ComputeValue(string input)
        {
            return _crc.ComputeValue(input);
        }

        public uint ComputeValue(string input, Encoding enc)
        {
            return _crc.ComputeValue(input, enc);
        }

        public uint ComputeValue(Stream input)
        {
            return _crc.ComputeValue(input);
        }

        public uint ComputeValue(Span<byte> input)
        {
            return _crc.ComputeValue(input);
        }
    }
}
