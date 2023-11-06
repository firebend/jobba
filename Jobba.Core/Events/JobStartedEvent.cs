using System;

namespace Jobba.Core.Events;

public class JobStartedEvent
{
    public JobStartedEvent()
    {
    }

    public JobStartedEvent(Guid jobId, Guid jobRegistrationId)
    {
        JobId = jobId;
        JobRegistrationId = jobRegistrationId;
    }

    /// <summary>
    /// The job id.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }
}
