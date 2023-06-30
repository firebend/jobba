using System;

namespace Jobba.Core.Events;

public class JobFaultedEvent
{
    public JobFaultedEvent()
    {
    }

    public JobFaultedEvent(Guid jobId)
    {
        JobId = jobId;
    }

    /// <summary>
    ///     The id of the job that has faulted.
    /// </summary>
    public Guid JobId { get; set; }
}
