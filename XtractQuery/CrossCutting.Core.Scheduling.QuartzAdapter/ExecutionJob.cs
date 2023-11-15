using System;
using System.Threading.Tasks;
using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.Logging;
using CrossCutting.Core.Contract.Messages.Scheduling;
using CrossCutting.Core.Contract.Scheduling;
using Quartz;

namespace CrossCutting.Core.Scheduling.QuartzAdapter
{
    public class ExecutionJob : IJob
    {
        private readonly ILogger _logger;
        private readonly IEventBroker _eventBroker;

        public ExecutionJob(ILogger logger, IEventBroker eventBroker)
        {
            _logger = logger;
            _eventBroker = eventBroker;
        }

        public Task Execute(IJobExecutionContext context)
        {
            if (!context.MergedJobDataMap.TryGetValue("Action", out object action) || action is not Action job)
            {
                _logger.Error($"Could not get action value from job ({context}).");
                return Task.CompletedTask;
            }

            if (!context.MergedJobDataMap.TryGetValue("JobData", out object data) || data is not JobData jobData)
            {
                _logger.Error($"Could not get job data value from job ({context}).");
                return Task.CompletedTask;
            }

            DateTime startTime = DateTime.UtcNow;

            Task task = Task.Run(() =>
            {
                try
                {
                    _logger.Info($"Job with name \"{jobData.Name}\" started at {startTime}");
                    _eventBroker.Raise(new SchedulerEventStartedMessage() { StartTime = startTime, JobData = jobData });

                    job.Invoke();
                }
                catch (Exception e)
                {
                    _logger.Error($"Job with name \"{jobData.Name}\" threw an exception.", e);
                    _eventBroker.Raise(new SchedulerEventExceptionMessage() { Exception = e, Name = jobData.Name, JobData = jobData });
                }
            }).ContinueWith((t) =>
            {
                try
                {
                    TimeSpan duration = DateTime.UtcNow.Subtract(startTime);

                    _logger.Info($"Job with name \"{jobData.Name}\" finished. Duration: {duration}");
                    _eventBroker.Raise(new SchedulerEventFinishedMessage() { StartTime = startTime, Duration = duration, NextRun = context.NextFireTimeUtc?.UtcDateTime, JobData = jobData });
                }
                catch (Exception e)
                {
                    _logger.Error($"Job with name \"{jobData.Name}\" threw an exception.", e);
                    _eventBroker.Raise(new SchedulerEventExceptionMessage() { Exception = e, Name = jobData.Name, JobData = jobData });
                }
            });

            return task;
        }
    }
}
