using CrossCutting.Core.Contract.Bootstrapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using CrossCutting.Core.Contract.EventBrokerage;
using Logic.Domain.Kuriimu2.KompressionAdapter.Contract;
using Logic.Domain.Kuriimu2.KompressionAdapter.InternalContract;

namespace Logic.Domain.Kuriimu2.KompressionAdapter
{
    public class Kuriimu2KompressionActivator : IComponentActivator
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
            kernel.Register<ICompressionFactory, CompressionFactory>(ActivationScope.Unique);

            kernel.Register<ILevel5Lz10Compression, Level5Lz10Compression>(ActivationScope.Unique);
            kernel.Register<ILevel5Huffman4BitCompression, Level5Huffman4BitCompression>(ActivationScope.Unique);
            kernel.Register<ILevel5Huffman8BitCompression, Level5Huffman8BitCompression>(ActivationScope.Unique);
            kernel.Register<ILevel5RleCompression, Level5RleCompression>(ActivationScope.Unique);

            kernel.Register<IZLibCompression, ZLibCompression>(ActivationScope.Unique);

            kernel.RegisterConfiguration<Kuriimu2KompressionConfiguration>();
        }

        public void AddMessageSubscriptions(IEventBroker broker)
        {
        }

        public void Configure(IConfigurator configurator)
        {
        }
    }
}
