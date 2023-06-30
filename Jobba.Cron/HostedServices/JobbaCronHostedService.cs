using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Cron.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.Cron.HostedServices;

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

        using var timer = new PeriodicTimer(timerDuration);

        await DoWorkAsync(timerDuration, stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DoWorkAsync(timerDuration, stoppingToken);
        }
    }

    private async Task DoWorkAsync(TimeSpan timerDuration, CancellationToken stoppingToken)
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.Add(timerDuration);

        _logger.LogDebug("Looking for jobs to run {Start} {End}", start, end);

        using var scope = _scopeFactory.CreateScope();
        var scheduler = scope.ServiceProvider.GetService<ICronScheduler>();

        if (scheduler is null)
        {
            _logger.LogCritical("No {Scheduler} is registered", nameof(ICronScheduler));
            return;
        }

        await scheduler.EnqueueJobsAsync(scope, start, end, stoppingToken);
    }
}
