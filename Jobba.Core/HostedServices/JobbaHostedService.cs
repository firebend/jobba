using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Repositories;
using Jobba.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.HostedServices;

public class JobbaHostedService : BackgroundService
{
    private readonly ILogger<JobbaHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    private static readonly CancellationTokenSource HasRegisteredJobsCancellationTokenSource = new();

    public static CancellationToken HasRegisteredJobsCancellationToken
        => HasRegisteredJobsCancellationTokenSource.Token;

    public JobbaHostedService(ILogger<JobbaHostedService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug("Jobba Hosted Service is running");

        if (!_scopeFactory.TryCreateScope(out var scope))
        {
            return;
        }

        using var serviceScope = scope;

        await RegisterJobsFromStoreAsync(scope, stoppingToken);
        await RestartFaultedJobsAsync(scope, stoppingToken);

        stoppingToken.Register(CancelAllJobs);
    }

    private async Task RegisterJobsFromStoreAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        var registrations = scope.ServiceProvider.GetServices<JobRegistration>().ToArray();

        if(registrations.Length == 0)
        {
            _logger.LogInformation("There are job definitions for Jobba to register");
            return;
        }

        if(scope.ServiceProvider.TryGetService<IJobRegistrationStore>(out var store) is false || store is null)
        {
            return;
        }

        foreach (var job in registrations)
        {
            var saved = await store.RegisterJobAsync(job, stoppingToken);
            job.Id = saved.Id;

            _logger.LogInformation("Jobba registered job {JobName} with id {JobId}", job.JobName, job.Id);
        }

        HasRegisteredJobsCancellationTokenSource.Cancel();
    }

    private async Task RestartFaultedJobsAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        if (scope.ServiceProvider.TryGetService<IJobReScheduler>(out var jobScheduler) is false || jobScheduler is null)
        {
            return;
        }

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

    private void CancelAllJobs()
    {
        _logger.LogInformation("Jobba is cancelling all jobs");

        if (!_scopeFactory.TryCreateScope(out var scope))
        {
            return;
        }

        using var serviceScope = scope;

        if (scope.ServiceProvider.TryGetService<IJobCancellationTokenStore>(out var cancellationTokenStore))
        {
            cancellationTokenStore?.CancelAllJobs();
        }
    }
}
