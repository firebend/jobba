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
    private readonly TimeSpan _timerDelay = TimeSpan.FromSeconds(15);

    public JobbaCronHostedService(ILogger<JobbaCronHostedService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Jobba Cron Hosted Service is starting. Checking for jobs every {Time}", _timerDelay);

        await CenterTimerAsync(stoppingToken);

        using var timer = new PeriodicTimer(_timerDelay);

        await DoWorkAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DoWorkAsync(stoppingToken);
        }

        _logger.LogInformation("Jobba Cron Hosted service is stopping");
    }

    private static async Task CenterTimerAsync(CancellationToken stoppingToken)
    {
        while (DateTimeOffset.UtcNow.Second % 15 != 0)
        {
            await Task.Delay(500, stoppingToken);
        }
    }

    private async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        var max = DateTimeOffset.Now.TrimMilliseconds();
        var min = max.Subtract(_timerDelay);

        using var scope = _scopeFactory.CreateScope();
        var scheduler = scope.ServiceProvider.GetService<ICronScheduler>();

        if (scheduler is null)
        {
            _logger.LogCritical("No {Scheduler} is registered", nameof(ICronScheduler));
            return;
        }

        await scheduler.EnqueueJobsAsync(scope, min, max, stoppingToken);
    }
}
