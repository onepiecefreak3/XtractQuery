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
    internal class ScriptCompressorFactory : IScriptCompressorFactory
    {
        private readonly ICoCoKernel _kernel;

        public ScriptCompressorFactory(ICoCoKernel kernel)
        {
            _kernel = kernel;
        }

        public IScriptCompressor Create(ScriptType type)
        {
            switch (type)
            {
                case ScriptType.Xq32:
                    return _kernel.Get<IXq32ScriptCompressor>();

                case ScriptType.Xseq:
                    return _kernel.Get<IXseqScriptCompressor>();

                default:
                    throw new InvalidOperationException($"Unknown script type {type}.");
            }
        }
    }
}
