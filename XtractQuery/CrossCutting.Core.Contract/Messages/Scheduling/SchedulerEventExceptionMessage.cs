using System;
using CrossCutting.Core.Contract.Scheduling;

namespace CrossCutting.Core.Contract.Messages.Scheduling;

public class SchedulerEventExceptionMessage
{
    /// <summary>Name of the job.</summary>
    public string Name { get; set; }

    /// <summary>Job's exception.</summary>
    public Exception Exception { get; set; }

    public JobData JobData { get; set; }
}