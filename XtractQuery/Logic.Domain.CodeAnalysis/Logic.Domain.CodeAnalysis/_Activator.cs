using CrossCutting.Core.Contract.Bootstrapping;
using CrossCutting.Core.Contract.Configuration;
using CrossCutting.Core.Contract.DependencyInjection;
using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.DependencyInjection.DataClasses;
using Logic.Domain.CodeAnalysis.Contract;
using Logic.Domain.CodeAnalysis.Contract.Level5;
using Logic.Domain.CodeAnalysis.Level5;
using Logic.Domain.CodeAnalysis.Level5.InternalContract;
using Logic.Domain.CodeAnalysis.Level5.InternalContract.DataClasses;
using Microsoft.CodeAnalysis;

namespace Logic.Domain.CodeAnalysis
{
    public class CodeAnalysisActivator : IComponentActivator
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
            kernel.Register<ITokenFactory<Level5SyntaxToken>, Level5ScriptFactory>(ActivationScope.Unique);
            kernel.Register<ILexer<Level5SyntaxToken>, Level5ScriptLexer>();
            kernel.Register<IBuffer<Level5SyntaxToken>, TokenBuffer<Level5SyntaxToken>>();
            kernel.Register<IBuffer<int>, StringBuffer>();

            kernel.Register<ILevel5ScriptParser, Level5ScriptParser>(ActivationScope.Unique);
            kernel.Register<ILevel5ScriptComposer, Level5ScriptComposer>(ActivationScope.Unique);
            kernel.Register<ILevel5ScriptWhitespaceNormalizer, Level5ScriptWhitespaceNormalizer>(ActivationScope.Unique);

            kernel.Register<ILevel5SyntaxFactory, Level5SyntaxFactory>();

            kernel.RegisterConfiguration<CodeAnalysisConfiguration>();
        }

        public void AddMessageSubscriptions(IEventBroker broker)
        {
        }

        public void Configure(IConfigurator configurator)
        {
        }
    }
}
