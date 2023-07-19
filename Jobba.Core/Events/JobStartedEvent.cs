using System;

namespace Jobba.Core.Events;

public class JobStartedEvent
{
    public JobStartedEvent()
    {
    }

    public JobStartedEvent(Guid jobId)
    {
        JobId = jobId;
    }

    public Guid JobId { get; set; }
}
