using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.HostedServices;

public class JobbaCleanUpHostedService : AbstractJobbaDependentBackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once MemberCanBePrivate.Global
#pragma warning disable CA2211
#pragma warning disable IDE1006
    public static TimeSpan CleanUpDuration = TimeSpan.FromDays(30);
#pragma warning restore IDE1006
#pragma warning restore CA2211

    public JobbaCleanUpHostedService(IServiceScopeFactory scopeFactory,
        ILogger<JobbaCleanUpHostedService> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task DoWorkAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        await TimerTickAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TimerTickAsync(stoppingToken);
        }
    }

    private async Task TimerTickAsync(CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IJobCleanUpStore>();
        await service.CleanUpJobsAsync(CleanUpDuration, cancellationToken);
    }
}
