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
        _logger.LogInformation("Jobba Hosted Service has started");

        if (!_scopeFactory.TryCreateScope(out var scope))
        {
            return;
        }

        using var serviceScope = scope;

        try
        {
            var gates = scope.ServiceProvider.GetServices<IJobbaReadyGate>().ToArray();
            if (gates.Length > 0)
            {
                stoppingToken.ThrowIfCancellationRequested();
                await Task.WhenAll(gates.Select(g => g.WaitAsync(stoppingToken)));
            }

            _logger.LogInformation("Jobba Hosted Service is ready to initialize");

            await RegisterJobsFromStoreAsync(scope, stoppingToken);
            await RestartFaultedJobsAsync(scope, stoppingToken);

            _logger.LogInformation("Jobba Hosted Service has completed initialization and is now running");

            stoppingToken.Register(CancelAllJobs);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected when cancellation is requested
        }
    }

    private async Task RegisterJobsFromStoreAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        var registrations = scope.ServiceProvider.GetServices<JobRegistration>().ToArray();

        if (registrations.Length == 0)
        {
            _logger.LogInformation("There are no job definitions for Jobba to register");
            HasRegisteredJobsCancellationTokenSource.Cancel();
            return;
        }

        if (scope.ServiceProvider.TryGetService<IJobRegistrationStore>(out var store) is false || store is null)
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
            _logger.LogInformation("Jobba is restarting faulted jobs");

            await jobScheduler.RestartFaultedJobsAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error trying to restart failed jobs");
            throw;
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
