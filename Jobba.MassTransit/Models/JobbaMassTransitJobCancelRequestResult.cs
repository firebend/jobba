using System;

namespace Jobba.MassTransit.Models;

public class JobbaMassTransitJobCancelRequestResult<TJob>
{
    public bool WasCancelled { get; set; }
    public Guid JobId { get; set; }
}
