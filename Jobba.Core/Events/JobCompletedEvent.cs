using System;

namespace Jobba.Core.Events;

public class JobCompletedEvent
{
    public JobCompletedEvent()
    {
    }

    public JobCompletedEvent(Guid jobId, Guid jobRegistrationId)
    {
        JobId = jobId;
        JobRegistrationId = jobRegistrationId;
    }

    /// <summary>
    ///     The id of the job that was completed.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }
}
