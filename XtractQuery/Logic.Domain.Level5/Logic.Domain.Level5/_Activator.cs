using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using Logic.Domain.Level5.Compression;
using Logic.Domain.Level5.Compression.InternalContract;
using Logic.Domain.Level5.Script;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.Gds;
using Logic.Domain.Level5.Contract.Script.Gsd1;
using Logic.Domain.Level5.Contract.Script.Gss1;
using Logic.Domain.Level5.Contract.Script.Xq32;
using Logic.Domain.Level5.Contract.Script.Xscr;
using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Domain.Level5.Cryptography;
using Logic.Domain.Level5.Cryptography.InternalContract;
using Logic.Domain.Level5.Script.Gds;
using Logic.Domain.Level5.Script.Gsd1;
using Logic.Domain.Level5.Script.Xq32.InternalContract;
using Logic.Domain.Level5.Script.Xseq.InternalContract;
using Logic.Domain.Level5.Script.Xq32;
using Logic.Domain.Level5.Script.Xseq;
using Logic.Domain.Level5.Script.Gss1;
using Logic.Domain.Level5.Script.Xscr;

namespace Logic.Domain.Level5;

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

        kernel.Register<IStringTableFactory, StringTableFactory>(ActivationScope.Unique);
        kernel.Register<IScriptEntrySizeProviderFactory, ScriptEntrySizeProviderFactory>(ActivationScope.Unique);

        kernel.Register<IXq32ScriptDecompressor, Xq32ScriptDecompressor>(ActivationScope.Unique);
        kernel.Register<IXq32ScriptCompressor, Xq32ScriptCompressor>(ActivationScope.Unique);
        kernel.Register<IXq32ScriptReader, Xq32ScriptReader>(ActivationScope.Unique);
        kernel.Register<IXq32ScriptWriter, Xq32ScriptWriter>(ActivationScope.Unique);
        kernel.Register<IXq32ScriptEntrySizeProvider, Xq32ScriptEntrySizeProvider>(ActivationScope.Unique);

        kernel.Register<IXq32StringTable, Xq32StringTable>();

        kernel.Register<IXseqScriptDecompressor, XseqScriptDecompressor>(ActivationScope.Unique);
        kernel.Register<IXseqScriptCompressor, XseqScriptCompressor>(ActivationScope.Unique);
        kernel.Register<IXseqScriptReader, XseqScriptReader>(ActivationScope.Unique);
        kernel.Register<IXseqScriptWriter, XseqScriptWriter>(ActivationScope.Unique);
        kernel.Register<IXseqScriptEntrySizeProvider, XseqScriptEntrySizeProvider>(ActivationScope.Unique);

        kernel.Register<IXseqStringTable, XseqStringTable>();

        kernel.Register<IGss1ScriptReader, Gss1ScriptReader>(ActivationScope.Unique);
        kernel.Register<IGss1ScriptParser, Gss1ScriptParser>(ActivationScope.Unique);
        kernel.Register<IGss1ScriptComposer, Gss1ScriptComposer>(ActivationScope.Unique);
        kernel.Register<IGss1ScriptWriter, Gss1ScriptWriter>(ActivationScope.Unique);

        kernel.Register<IGsd1ScriptReader, Gsd1ScriptReader>(ActivationScope.Unique);
        kernel.Register<IGsd1ScriptParser, Gsd1ScriptParser>(ActivationScope.Unique);
        kernel.Register<IGsd1ScriptComposer, Gsd1ScriptComposer>(ActivationScope.Unique);
        kernel.Register<IGsd1ScriptWriter, Gsd1ScriptWriter>(ActivationScope.Unique);

        kernel.Register<IXscrScriptDecompressor, XscrScriptDecompressor>(ActivationScope.Unique);
        kernel.Register<IXscrScriptCompressor, XscrScriptCompressor>(ActivationScope.Unique);
        kernel.Register<IXscrScriptReader, XscrScriptReader>(ActivationScope.Unique);
        kernel.Register<IXscrScriptParser, XscrScriptParser>(ActivationScope.Unique);
        kernel.Register<IXscrScriptComposer, XscrScriptComposer>(ActivationScope.Unique);
        kernel.Register<IXscrScriptWriter, XscrScriptWriter>(ActivationScope.Unique);

        kernel.Register<IGdsScriptReader, GdsScriptReader>(ActivationScope.Unique);
        kernel.Register<IGdsScriptParser, GdsScriptParser>(ActivationScope.Unique);
        kernel.Register<IGdsScriptComposer, GdsScriptComposer>(ActivationScope.Unique);
        kernel.Register<IGdsScriptWriter, GdsScriptWriter>(ActivationScope.Unique);

        kernel.RegisterConfiguration<Level5Configuration>();
    }

    public void AddMessageSubscriptions(IEventBroker broker)
    {
    }

    public void Configure(IConfigurator configurator)
    {
    }
}