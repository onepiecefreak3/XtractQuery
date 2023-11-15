using System;
using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.Logging;
using Quartz;
using Quartz.Spi;

namespace CrossCutting.Core.Scheduling.QuartzAdapter
{
    public class ExecutionJobFactory : IJobFactory
    {
        private readonly ILogger _logger;
        private readonly IEventBroker _eventBroker;

        public ExecutionJobFactory(ILogger logger, IEventBroker eventBroker)
        {
            _logger = logger;
            _eventBroker = eventBroker;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return new ExecutionJob(_logger, _eventBroker);
        }

        public void ReturnJob(IJob job)
        {
            IDisposable disposable = job as IDisposable;
            disposable?.Dispose();
        }
    }
}
