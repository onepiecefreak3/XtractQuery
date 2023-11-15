using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kryptography.Hash.Crc;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Contract;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum.InternalContract;
using Logic.Domain.Kuriimu2.KryptographyAdapter.Checksum;

namespace Logic.Domain.Kuriimu2.KryptographyAdapter
{
    public class Kuriimu2KryptographyActivator : IComponentActivator
    {
        public void Activating()
        {
        }

        public void Activated()
        {
        }

        public void Deactivating()
        {
        }

        public void Deactivated()
        {
        }

        public void Register(ICoCoKernel kernel)
        {
            kernel.Register<ICrcChecksumFactory, CrcChecksumFactory>();

            kernel.Register<ICrc32StandardChecksum, Crc32StandardChecksum>();
            kernel.Register<ICrc32BChecksum, Crc32BChecksum>();
            kernel.Register<ICrc32CChecksum, Crc32CChecksum>();
            kernel.Register<IJamCrcChecksum, JamCrcChecksum>();
            kernel.Register<ICrc16X25Checksum, Crc16X25Checksum>();
            kernel.Register<ICrc16ModBusChecksum, Crc16ModBusChecksum>();

            kernel.RegisterConfiguration<Kuriimu2KryptographyConfiguration>();
        }

        public void AddMessageSubscriptions(IEventBroker broker)
        {
        }

        public void Configure(IConfigurator configurator)
        {
        }
    }
}
