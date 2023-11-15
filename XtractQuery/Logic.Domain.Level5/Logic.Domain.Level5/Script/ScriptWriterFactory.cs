using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.DependencyInjection;
using Logic.Domain.Level5.Contract.Script;
using Logic.Domain.Level5.Contract.Script.DataClasses;
using Logic.Domain.Level5.Contract.Script.Xq32;
using Logic.Domain.Level5.Contract.Script.Xseq;
using Logic.Domain.Level5.Script.Xq32.InternalContract;
using Logic.Domain.Level5.Script.Xseq.InternalContract;

namespace Logic.Domain.Level5.Script
{
    internal class ScriptWriterFactory: IScriptWriterFactory
    {
        private readonly ICoCoKernel _kernel;

        public ScriptWriterFactory(ICoCoKernel kernel)
        {
            _kernel = kernel;
        }

        public IScriptWriter Create(ScriptType type)
        {
            switch (type)
            {
                case ScriptType.Xq32:
                    return _kernel.Get<IXq32ScriptWriter>();

                case ScriptType.Xseq:
                    return _kernel.Get<IXseqScriptWriter>();

                default:
                    throw new InvalidOperationException($"Unknown script type {type}.");
            }
        }
    }
}
