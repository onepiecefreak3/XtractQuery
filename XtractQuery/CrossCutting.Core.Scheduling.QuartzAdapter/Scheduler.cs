using System;
using System.Collections.Generic;
using CrossCutting.Core.Contract.EventBrokerage;
using CrossCutting.Core.Contract.Logging;
using CrossCutting.Core.Contract.Scheduling;
using Quartz;
using Quartz.Impl;

namespace CrossCutting.Core.Scheduling.QuartzAdapter
{
    public class Scheduler : Contract.Scheduling.IScheduler
    {
        private readonly ILogger _logger;
        private readonly global::Quartz.IScheduler _scheduler;

        public Scheduler(ILogger logger, IEventBroker eventBroker)
        {
            _scheduler = new StdSchedulerFactory().GetScheduler().Result;
            _scheduler.JobFactory = new ExecutionJobFactory(logger, eventBroker);
            _scheduler.Start().Wait();

            _logger = logger;
        }

        public JobData AddJobRunOnce(Action job, SchedulerIntervalUnit intervalType, uint interval)
        {
            return AddJobRunOnce(job, intervalType, interval, string.Empty);
        }

        public JobData AddJobRunOnce(Action job, SchedulerIntervalUnit intervalType, uint interval, string name)
        {
            JobData result = CreateJobData(name);
            string jobName = GetJobName(result);

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(jobName)
                .StartAt(DateBuilder.FutureDate((int)interval, GetIntervalUnit(intervalType)))
                .Build();

            _scheduler.ScheduleJob(GetJobDetail(job, result), trigger).Wait();

            _logger.Debug($"Job added (run once). Name: \"{jobName}\"  interval: {interval} {intervalType}");

            return result;
        }

        public JobData AddJobRunOnce(Action job, DateTime startDate)
        {
            return AddJobRunOnce(job, startDate, string.Empty);
        }

        public JobData AddJobRunOnce(Action job, DateTime startDate, string name)
        {
            JobData result = CreateJobData(name);
            string jobName = GetJobName(result);

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(jobName)
                .StartAt(new DateTimeOffset(startDate))
                .Build();

            _scheduler.ScheduleJob(GetJobDetail(job, result), trigger).Wait();

            _logger.Debug($"Job added (run once). Name: \"{jobName}\"  start date: {startDate}");

            return result;
        }

        public JobData AddJob(Action job, SchedulerIntervalUnit intervalType, uint interval, bool startImmediately)
        {
            return AddJob(job, intervalType, interval, startImmediately, string.Empty);
        }

        public JobData AddJob(Action job, SchedulerIntervalUnit intervalType, uint interval, bool startImmediately, string name)
        {
            JobData result = CreateJobData(name);
            string jobName = GetJobName(result);

            TriggerBuilder triggerBuilder = TriggerBuilder.Create()
                .WithIdentity(jobName)
                .WithCalendarIntervalSchedule(x => x.WithInterval((int)interval, GetIntervalUnit(intervalType)));

            if (!startImmediately)
            {
                triggerBuilder.StartAt(DateBuilder.FutureDate((int)interval, GetIntervalUnit(intervalType)));
            }

            _scheduler.ScheduleJob(GetJobDetail(job, result), triggerBuilder.Build()).Wait();

            if (startImmediately)
                _logger.Debug($"Job added (run now and recurring). Name: \"{jobName}\"  interval: {interval} {intervalType}");
            else
                _logger.Debug($"Job added (recurring). Name: \"{jobName}\"  interval: {interval} {intervalType}");

            return result;
        }

        public JobData AddJob(Action job, SchedulerIntervalUnit intervalType, uint interval, DateTime startDate, string name)
        {
            JobData result = CreateJobData(name);
            string jobName = GetJobName(result);

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(jobName)
                .StartAt(new DateTimeOffset(startDate))
                .WithCalendarIntervalSchedule(x => x.WithInterval((int)interval, GetIntervalUnit(intervalType)))
                .Build();

            _scheduler.ScheduleJob(GetJobDetail(job, result), trigger).Wait();

            _logger.Debug($"Job added (recurring). Name: \"{jobName}\" start date: {startDate:yyyy-MM-dd HH:mm:ss} interval: {interval} {intervalType}");

            return result;
        }

        public void RemoveJob(JobData jobData)
        {
            string jobName = GetJobName(jobData);

            if (_scheduler.UnscheduleJob(new TriggerKey(jobName)).Result)
            {
                _logger.Debug($"Job removed. Name: \"{jobName}\"");
            }
            else
            {
                _logger.Error($"Error removing job. Name: \"{jobName}\"");
            }
        }

        private JobData CreateJobData(string name)
        {
            return new JobData(name, Guid.NewGuid());
        }

        private string GetJobName(JobData jobData)
        {
            return string.IsNullOrWhiteSpace(jobData.Name) ? $"{jobData.Guid}" : $"{jobData.Name}-{jobData.Guid}";
        }

        private IntervalUnit GetIntervalUnit(SchedulerIntervalUnit intervalUnit)
        {
            return intervalUnit switch
            {
                SchedulerIntervalUnit.Milliseconds => IntervalUnit.Millisecond,
                SchedulerIntervalUnit.Seconds => IntervalUnit.Second,
                SchedulerIntervalUnit.Minutes => IntervalUnit.Minute,
                SchedulerIntervalUnit.Hours => IntervalUnit.Hour,
                SchedulerIntervalUnit.Days => IntervalUnit.Day,
                SchedulerIntervalUnit.Weeks => IntervalUnit.Week,
                SchedulerIntervalUnit.Months => IntervalUnit.Month,
                _ => throw new Exception("Unknown interval type")
            };
        }

        private IJobDetail GetJobDetail(Action job, JobData data)
        {
            IDictionary<string, object> jobData = new Dictionary<string, object>
            {
                { "Action", job },
                { "JobData", data }
            };

            return JobBuilder.Create<ExecutionJob>()
                .WithIdentity(GetJobName(data))
                .SetJobData(new JobDataMap(jobData))
                .Build();
        }
    }
}
