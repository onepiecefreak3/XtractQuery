using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.EventBrokerage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using Logic.Domain.Level5.Compression;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Script;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.Xq32;
using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Domain.Level5.Cryptography;
using Logic.Domain.Level5.Cryptography.InternalContract;
using Logic.Domain.Level5.Script.Xq32.InternalContract;
using Logic.Domain.Level5.Script.Xseq.InternalContract;
using Logic.Domain.Level5.Script.Xq32;
using Logic.Domain.Level5.Script.Xseq;

namespace Logic.Domain.Level5
{
    public class Level5Activator : IComponentActivator
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
            kernel.Register<ICompressor, Compressor>(ActivationScope.Unique);
            kernel.Register<IDecompressor, Decompressor>(ActivationScope.Unique);

            kernel.Register<IChecksumFactory, ChecksumFactory>(ActivationScope.Unique);

            kernel.Register<IScriptTypeReader, ScriptTypeReader>(ActivationScope.Unique);

            kernel.Register<IScriptDecompressorFactory, ScriptDecompressorFactory>(ActivationScope.Unique);
            kernel.Register<IScriptCompressorFactory, ScriptCompressorFactory>(ActivationScope.Unique);
            kernel.Register<IStringTableFactory, StringTableFactory>(ActivationScope.Unique);
            kernel.Register<IScriptReaderFactory, ScriptReaderFactory>(ActivationScope.Unique);
            kernel.Register<IScriptWriterFactory, ScriptWriterFactory>(ActivationScope.Unique);

            kernel.Register<IXq32ScriptDecompressor, Xq32ScriptDecompressor>();
            kernel.Register<IXq32ScriptCompressor, Xq32ScriptCompressor>();
            kernel.Register<IXq32StringTable, Xq32StringTable>();
            kernel.Register<IXq32ScriptReader, Xq32ScriptReader>();
            kernel.Register<IXq32ScriptWriter, Xq32ScriptWriter>();
            kernel.Register<IXq32ScriptHashStringCache, Xq32ScriptHashStringCache>(ActivationScope.Unique);

            kernel.Register<IXseqScriptDecompressor, XseqScriptDecompressor>();
            kernel.Register<IXseqScriptCompressor, XseqScriptCompressor>();
            kernel.Register<IXseqStringTable, XseqStringTable>();
            kernel.Register<IXseqScriptReader, XseqScriptReader>();
            kernel.Register<IXseqScriptWriter, XseqScriptWriter>();
            kernel.Register<IXseqScriptHashStringCache, XseqScriptHashStringCache>(ActivationScope.Unique);

            kernel.RegisterConfiguration<Level5Configuration>();
        }

        public void AddMessageSubscriptions(IEventBroker broker)
        {
        }

        public void Configure(IConfigurator configurator)
        {
        }
    }
}
