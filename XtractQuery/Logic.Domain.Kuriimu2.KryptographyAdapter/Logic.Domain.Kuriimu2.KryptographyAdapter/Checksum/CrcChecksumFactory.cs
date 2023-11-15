using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.DependencyInjection;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract.DataClasses;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum
{
    internal class CrcChecksumFactory : ICrcChecksumFactory
    {
        private readonly ICoCoKernel _kernel;

        public CrcChecksumFactory(ICoCoKernel kernel)
        {
            _kernel = kernel;
        }

        public IChecksum<uint> CreateCrc32(Crc32Type type)
        {
            switch (type)
            {
                case Crc32Type.Standard:
                    return _kernel.Get<ICrc32StandardChecksum>();

                case Crc32Type.Crc32B:
                    return _kernel.Get<ICrc32BChecksum>();

                case Crc32Type.Crc32C:
                    return _kernel.Get<ICrc32CChecksum>();

                case Crc32Type.JamCrc:
                    return _kernel.Get<IJamCrcChecksum>();

                default:
                    throw new InvalidOperationException($"Unknown CRC32 type {type}.");
            }
        }

        public IChecksum<ushort> CreateCrc16(Crc16Type type)
        {
            switch (type)
            {
                case Crc16Type.X25:
                    return _kernel.Get<ICrc16X25Checksum>();

                case Crc16Type.ModBus:
                    return _kernel.Get<ICrc16ModBusChecksum>();

                default:
                    throw new InvalidOperationException($"Unknown CRC16 type {type}.");
            }
        }
    }
}
