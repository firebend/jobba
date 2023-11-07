using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.HostedServices;

public abstract class AbstractJobbaDependentBackgroundService : BackgroundService
{
    protected virtual ILogger Logger { get; }

    protected AbstractJobbaDependentBackgroundService(ILogger logger)
    {
        Logger = logger;
    }

    protected virtual async Task<bool> WaitForJobbaAsync(CancellationToken stoppingToken)
    {
        Logger.LogDebug("Waiting for jobba to register jobs before continuing");

        var startedSource = new TaskCompletionSource();
        var cancelledSource = new TaskCompletionSource();

        await using var jobbaToken = JobbaHostedService.HasRegisteredJobsCancellationToken.Register(() => startedSource.SetResult());
        await using var hostedServiceToken = stoppingToken.Register(() => cancelledSource.SetResult());

        var completedTask = await Task.WhenAny(
            startedSource.Task,
            cancelledSource.Task);

        var hasJobbaStarted = completedTask == startedSource.Task;

        Logger.LogDebug("Done waiting {HasStarted}", hasJobbaStarted);

        return hasJobbaStarted;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await WaitForJobbaAsync(stoppingToken);
        await DoWorkAsync(stoppingToken);
    }

    protected abstract Task DoWorkAsync(CancellationToken stoppingToken);
}
