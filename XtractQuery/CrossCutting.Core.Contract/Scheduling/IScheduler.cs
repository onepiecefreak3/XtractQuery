using System;

namespace CrossCutting.Core.Contract.Scheduling
{
    public interface IScheduler
    {
        /// <summary>
        /// Adds a job to run once after a given interval
        /// </summary>
        /// <param name="job">Job to be executed</param>
        /// <param name="intervalType">seconds, minutes, etc.</param>
        /// <param name="interval">amount of seconds, minutes, etc.</param>
        public JobData AddJobRunOnce(Action job, SchedulerIntervalUnit intervalType, uint interval);

        /// <summary>
        /// Adds a job to run once after a given interval
        /// </summary>
        /// <param name="job">Job to be executed</param>
        /// <param name="intervalType">seconds, minutes, etc.</param>
        /// <param name="interval">amount of seconds, minutes, etc.</param>
        /// <param name="name">name of job</param>
        public JobData AddJobRunOnce(Action job, SchedulerIntervalUnit intervalType, uint interval, string name);

        /// <summary>
        /// Adds a job to run once at a given date
        /// </summary>
        /// <param name="job">Job to be executed</param>
        /// <param name="startDate">date and time when the job should be executed</param>
        public JobData AddJobRunOnce(Action job, DateTime startDate);

        /// <summary>
        /// Adds a job to run once at a given date
        /// </summary>
        /// <param name="job">Job to be executed</param>
        /// <param name="startDate">date and time when the job should be executed</param>
        /// <param name="name">name of job</param>
        public JobData AddJobRunOnce(Action job, DateTime startDate, string name);

        /// <summary>
        /// Adds a job to run immediately and reruns after a given interval
        /// </summary>
        /// <param name="job">Job to be executed</param>
        /// <param name="intervalType">seconds, minutes, etc.</param>
        /// <param name="interval">amount of seconds, minutes, etc.</param>
        /// <param name="startImmediately">whether job should run immediately once before the interval is taken into account</param>
        public JobData AddJob(Action job, SchedulerIntervalUnit intervalType, uint interval, bool startImmediately);

        /// <summary>
        /// Adds a job to run immediately and reruns after a given interval
        /// </summary>
        /// <param name="job">Job to be executed</param>
        /// <param name="intervalType">seconds, minutes, etc.</param>
        /// <param name="interval">amount of seconds, minutes, etc.</param>
        /// <param name="startImmediately">whether job should run immediately once before the interval is taken into account</param>
        /// <param name="name">name of job</param>
        public JobData AddJob(Action job, SchedulerIntervalUnit intervalType, uint interval, bool startImmediately, string name);

        /// <summary>
        /// Adds a job to run immediately and reruns after a given interval
        /// </summary>
        /// <param name="job">Job to be executed</param>
        /// <param name="intervalType">seconds, minutes, etc.</param>
        /// <param name="interval">amount of seconds, minutes, etc.</param>
        /// <param name="startDate">start date and time of first execution</param>
        /// <param name="name">name of job</param>
        public JobData AddJob(Action job, SchedulerIntervalUnit intervalType, uint interval, DateTime startDate, string name);

        /// <summary>
        /// Removes the job with the given job data
        /// </summary>
        /// <param name="jobData">job to be removed</param>
        public void RemoveJob(JobData jobData);
    }
}
