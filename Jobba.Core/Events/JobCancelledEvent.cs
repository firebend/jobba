using System;

namespace Jobba.Core.Events;

public class JobCancelledEvent
{
    public JobCancelledEvent()
    {
    }

    public JobCancelledEvent(Guid jobId, Guid jobRegistrationId)
    {
        JobId = jobId;
        JobRegistrationId = jobRegistrationId;
    }

    /// <summary>
    ///     The id of the job that was cancelled.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }
}
