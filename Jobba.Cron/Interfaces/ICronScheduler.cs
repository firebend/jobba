using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Cron.Interfaces;

public record CronSchedulerContext(TimeSpan Interval,
    DateTimeOffset Min,
    DateTimeOffset Max,
    string SystemMoniker)
{
    public override string ToString()
        => $"Interval: {Interval} Min: {Min} Max: {Max} System Moniker: {SystemMoniker}";
}

public interface ICronScheduler
{
    /// <summary>
    /// Give a service provider scope and a date all registered cron jobs will be resolved and executed.
    /// </summary>
    /// <param name="context">
    /// A context encapsulating information about when to check for cron job invocations.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token
    /// </param>
    /// <returns>
    /// A task
    /// </returns>
    public Task EnqueueJobsAsync(CronSchedulerContext context, CancellationToken cancellationToken);
}
