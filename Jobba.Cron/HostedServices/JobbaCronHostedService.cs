using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Cron.Extensions;
using Jobba.Cron.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.Cron.HostedServices;

/// <summary>
/// Responsible for queuing cron jobs.
/// </summary>
public class JobbaCronHostedService : BackgroundService
{
    private readonly ILogger<JobbaCronHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public JobbaCronHostedService(ILogger<JobbaCronHostedService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timerDuration = TimeSpan.FromSeconds(1);

        _logger.LogInformation("Jobba Cron Hosted Service is starting. Checking for jobs every {Time}", timerDuration);

        using var timer = new PeriodicTimer(timerDuration);

        await DoWorkAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DoWorkAsync(stoppingToken);
        }

        _logger.LogInformation("Jobba Cron Hosted service is stopping");
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        var now = DateTimeOffset.UtcNow.TrimMilliseconds();

        using var scope = _scopeFactory.CreateScope();
        var scheduler = scope.ServiceProvider.GetService<ICronScheduler>();

        if (scheduler is null)
        {
            _logger.LogCritical("No {Scheduler} is registered", nameof(ICronScheduler));
            return;
        }

        await scheduler.EnqueueJobsAsync(scope, now, stoppingToken);
    }
}
