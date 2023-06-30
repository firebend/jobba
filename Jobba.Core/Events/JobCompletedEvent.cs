using System;

namespace Jobba.Core.Events;

public class JobCompletedEvent
{
    public JobCompletedEvent()
    {
    }

    public JobCompletedEvent(Guid jobId)
    {
        JobId = jobId;
    }

    /// <summary>
    ///     The id of the job that was completed.
    /// </summary>
    public Guid JobId { get; set; }
}
