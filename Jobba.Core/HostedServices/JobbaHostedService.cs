using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.HostedServices;

public class JobbaHostedService : BackgroundService
{
    private readonly ILogger<JobbaHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public JobbaHostedService(ILogger<JobbaHostedService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Jobba Hosted Service is running");

        if (_scopeFactory.TryCreateScope(out var scope))
        {
            using (scope)
            {
                if (scope.ServiceProvider.TryGetService<IJobReScheduler>(out var jobScheduler))
                {
                    if (jobScheduler != null)
                    {
                        try
                        {
                            _logger.LogDebug("Jobba is restarting faulted jobs");

                            await jobScheduler.RestartFaultedJobsAsync(stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogCritical(ex, "Error trying to restart failed jobs");
                        }
                    }
                }
            }
        }

        stoppingToken.Register(CancelAllJobs);
    }

    private void CancelAllJobs()
    {
        _logger.LogInformation("Jobba is cancelling all jobs");

        if (_scopeFactory.TryCreateScope(out var scope))
        {
            using (scope)
            {
                if (scope.ServiceProvider.TryGetService<IJobCancellationTokenStore>(out var cancellationTokenStore))
                {
                    cancellationTokenStore?.CancelAllJobs();
                }
            }
        }
    }
}
