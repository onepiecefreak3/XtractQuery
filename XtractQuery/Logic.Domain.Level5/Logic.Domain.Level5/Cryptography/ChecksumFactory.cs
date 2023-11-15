using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Aspects;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract.DataClasses;
using Logic.Domain.Level5.Cryptography.InternalContract;
using Logic.Domain.Level5.Cryptography.InternalContract.Exceptions;

namespace Logic.Domain.Level5.Cryptography
{
    internal class ChecksumFactory : IChecksumFactory
    {
        private readonly ICrcChecksumFactory _crcFactory;

        public ChecksumFactory(ICrcChecksumFactory crcFactory)
        {
            _crcFactory = crcFactory;
        }

        public IChecksum<uint> CreateCrc32()
        {
            return _crcFactory.CreateCrc32(Crc32Type.Standard);
        }

        public IChecksum<ushort> CreateCrc16()
        {
            return _crcFactory.CreateCrc16(Crc16Type.X25);
        }
    }
}
