using System;

namespace Jobba.Core.Events;

public class JobFaultedEvent<TJob> : IJobbaEvent
{
    public JobFaultedEvent()
    {
    }

    public JobFaultedEvent(Guid jobId, Guid jobRegistrationId)
    {
        JobId = jobId;
        JobRegistrationId = jobRegistrationId;
    }

    /// <summary>
    ///     The id of the job that has faulted.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The job's registration id
    /// </summary>
    public Guid JobRegistrationId { get; set; }

    public string SystemMoniker { get; set; }
}
