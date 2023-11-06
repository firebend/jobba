using System;

namespace Jobba.Core.Events;

public class CancelJobEvent
{
    public CancelJobEvent()
    {
    }

    public CancelJobEvent(Guid jobId, Guid jobRegistrationId)
    {
        JobId = jobId;
        JobRegistrationId = jobRegistrationId;
    }

    /// <summary>
    ///     The id fo the job that needs to be cancelled. This is a job cancellation request.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }
}
