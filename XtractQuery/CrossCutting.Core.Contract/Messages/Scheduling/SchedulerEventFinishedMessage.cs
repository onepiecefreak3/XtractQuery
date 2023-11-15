using System;
using CrossCutting.Core.Contract.Scheduling;

namespace CrossCutting.Core.Contract.Messages.Scheduling
{
    public class SchedulerEventFinishedMessage
    {
        /// <summary>Date and time of the start.</summary>
        public DateTime StartTime { get; set; }

        /// <summary>The elapsed time of the job.</summary>
        public TimeSpan Duration { get; set; }

        /// <summary>Date and time of next run.</summary>
        public DateTime? NextRun { get; set; }

        public JobData JobData { get; set; }
    }
}
