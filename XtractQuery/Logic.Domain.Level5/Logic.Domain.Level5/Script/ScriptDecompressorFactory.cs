using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.DependencyInjection;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Script.Xq32.InternalContract;
using Logic.Domain.Level5.Script.Xseq.InternalContract;

namespace Logic.Domain.Level5.Script
{
    internal class ScriptDecompressorFactory : IScriptDecompressorFactory
    {
        private readonly ICoCoKernel _kernel;

        public ScriptDecompressorFactory(ICoCoKernel kernel)
        {
            _kernel = kernel;
        }

        public IScriptDecompressor Create(ScriptType type)
        {
            switch (type)
            {
                case ScriptType.Xq32:
                    return _kernel.Get<IXq32ScriptDecompressor>();

                case ScriptType.Xseq:
                    return _kernel.Get<IXseqScriptDecompressor>();

                default:
                    throw new InvalidOperationException($"Unknown script type {type}.");
            }
        }
    }
}
