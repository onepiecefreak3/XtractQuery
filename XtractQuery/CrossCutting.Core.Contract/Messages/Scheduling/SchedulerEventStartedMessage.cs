using System;
using CrossCutting.Core.Contract.Scheduling;

namespace CrossCutting.Core.Contract.Messages.Scheduling
{
    public class SchedulerEventStartedMessage
    {
        /// <summary>Date and time of the start.</summary>
        public DateTime StartTime { get; set; }

        public JobData JobData { get; set; }
    }
}
