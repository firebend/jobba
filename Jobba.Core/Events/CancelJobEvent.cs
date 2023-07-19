using System;

namespace Jobba.Core.Events;

public class CancelJobEvent
{
    public CancelJobEvent()
    {
    }

    public CancelJobEvent(Guid jobId)
    {
        JobId = jobId;
    }

    /// <summary>
    ///     The id fo the job that needs to be cancelled. This is a job cancellation request.
    /// </summary>
    public Guid JobId { get; set; }
}
