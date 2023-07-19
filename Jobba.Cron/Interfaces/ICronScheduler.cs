using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Cron.Interfaces;

public interface ICronScheduler
{
    /// <summary>
    /// Give a service provider scope and a date all registered cron jobs will be resolved and executed.
    /// </summary>
    /// <param name="scope">
    /// The service provider scope to resolve jobs from.
    /// </param>
    /// <param name="now">
    /// The date point in time reference to check jobs against
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token
    /// </param>
    /// <returns>
    /// A task
    /// </returns>
    Task EnqueueJobsAsync(IServiceScope scope, DateTimeOffset now, CancellationToken cancellationToken);
}
