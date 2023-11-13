using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Cron.Interfaces;

public interface ICronScheduler
{
    /// <summary>
    /// Give a service provider scope and a date all registered cron jobs will be resolved and executed.
    /// </summary>
    /// <param name="min">
    /// The minimum date point in time reference to check jobs against
    /// </param>
    /// <param name="max">
    /// The maximum date point in time reference to check jobs against
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token
    /// </param>
    /// <returns>
    /// A task
    /// </returns>
    Task EnqueueJobsAsync(DateTimeOffset min, DateTimeOffset max, CancellationToken cancellationToken);
}
