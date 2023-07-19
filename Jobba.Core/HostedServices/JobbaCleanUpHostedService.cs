using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Jobba.Core.HostedServices;

public class JobbaCleanUpHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    // ReSharper disable once InconsistentNaming
#pragma warning disable CA2211
    // ReSharper disable once MemberCanBePrivate.Global
    public static TimeSpan CleanUpDuration = TimeSpan.FromDays(30);
#pragma warning restore CA2211

    public JobbaCleanUpHostedService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        await DoWorkAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DoWorkAsync(stoppingToken);
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IJobCleanUpStore>();
        await service.CleanUpJobsAsync(CleanUpDuration, cancellationToken);
    }
}
