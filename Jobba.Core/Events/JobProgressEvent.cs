using System;

namespace Jobba.Core.Events;

public class JobProgressEvent
{
    public JobProgressEvent()
    {
    }

    public JobProgressEvent(Guid jobProgressId, Guid jobId, Guid jobRegistrationId)
    {
        JobProgressId = jobProgressId;
        JobId = jobId;
        JobRegistrationId = jobRegistrationId;
    }

    /// <summary>
    ///     An id pointing to the progress entity with progress information.
    /// </summary>
    public Guid JobProgressId { get; set; }

    /// <summary>
    ///     The Id of the job reporting progress
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }
}
