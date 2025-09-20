using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.Messages;
using CrossCutting.Core.Contract.DependencyInjection;
using XtractQuery;
using Logic.Business.Level5ScriptManagement.Contract;

KernelLoader loader = new();
ICoCoKernel kernel = loader.Initialize();

var eventBroker = kernel.Get<IEventBroker>();
eventBroker.Raise(new InitializeApplicationMessage());

var mainLogic = kernel.Get <IScriptManagementWorkflow>();
return mainLogic.Execute();
