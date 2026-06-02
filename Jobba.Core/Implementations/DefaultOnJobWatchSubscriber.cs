using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class DefaultOnJobWatchSubscriber<TJob, TJobParams, TJobState> : IOnJobWatchSubscriber<TJob, TJobParams, TJobState>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    private readonly ILogger<DefaultOnJobWatchSubscriber<TJob, TJobParams, TJobState>> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DefaultOnJobWatchSubscriber(ILogger<DefaultOnJobWatchSubscriber<TJob, TJobParams, TJobState>> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task WatchJobAsync(JobWatchEvent<TJob> jobWatchEvent, CancellationToken cancellationToken)
    {
        try
        {
            if (!_scopeFactory.TryCreateScope(out var scope))
            {
                return;
            }

            using (scope)
            {
                var watcher = scope.ServiceProvider.GetService<IJobWatcher<TJob, TJobParams, TJobState>>()
                              ?? scope.ServiceProvider.Materialize(typeof(DefaultJobWatcher<TJob, TJobParams, TJobState>)) as IJobWatcher<TJob, TJobParams, TJobState>
                              ?? throw new Exception($"Could not find job watcher for {typeof(TJobParams)} / {typeof(TJobState)}");

                await watcher.WatchJobAsync(jobWatchEvent.JobId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error watching jobs");
        }
    }
}
