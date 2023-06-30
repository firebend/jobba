using System;

namespace Jobba.Core.Events;

public class JobCancelledEvent
{
    public JobCancelledEvent()
    {
    }

    public JobCancelledEvent(Guid jobId)
    {
        JobId = jobId;
    }

    /// <summary>
    ///     The id of the job that was cancelled.
    /// </summary>
    public Guid JobId { get; set; }
}
