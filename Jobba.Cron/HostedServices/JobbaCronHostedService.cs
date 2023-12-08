using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.HostedServices;
using Jobba.Core.Interfaces;
using Jobba.Cron.Extensions;
using Jobba.Cron.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Cron.HostedServices;

/// <summary>
/// Responsible for queuing cron jobs.
/// </summary>
public class JobbaCronHostedService : AbstractJobbaDependentBackgroundService
{
    private readonly ILogger<JobbaCronHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeSpan _timerDelay = TimeSpan.FromSeconds(15);

    public JobbaCronHostedService(ILogger<JobbaCronHostedService> logger,
        IServiceScopeFactory scopeFactory) : base(logger)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Jobba Cron Hosted Service is starting. Checking for jobs every {Time}", _timerDelay);

            await CenterTimerAsync(stoppingToken);

            using var timer = new PeriodicTimer(_timerDelay);

            await TimerTickAsync(stoppingToken);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await TimerTickAsync(stoppingToken);
            }

            _logger.LogInformation("Jobba Cron Hosted service is stopping");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "Jobba Cron Hosted Service encountered a fatal error");
        }
    }

    private static async Task CenterTimerAsync(CancellationToken stoppingToken)
    {
        while (DateTimeOffset.UtcNow.Second % 15 != 0)
        {
            await Task.Delay(500, stoppingToken);
        }
    }

    private async Task TimerTickAsync(CancellationToken stoppingToken)
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

        var infoProvider = scope.ServiceProvider.GetService<IJobSystemInfoProvider>();

        if (infoProvider is null)
        {
            _logger.LogCritical("No {InfoProvider} is registered", nameof(IJobSystemInfoProvider));
            return;
        }

        var systemMoniker = infoProvider.GetSystemInfo().SystemMoniker;
        var context = new CronSchedulerContext(_timerDelay, min, max, systemMoniker);

        _logger.LogDebug("Enqueuing jobs between {Context}", context);

        await scheduler.EnqueueJobsAsync(context, stoppingToken);
    }
}
