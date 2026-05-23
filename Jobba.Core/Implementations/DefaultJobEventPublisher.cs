using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Extensions;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class DefaultJobEventPublisher : IJobEventPublisher, IDisposable
{
    private readonly ILogger<DefaultJobEventPublisher> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DefaultJobEventPublisher(ILogger<DefaultJobEventPublisher> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }


    public Task PublishJobCancellationRequestAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobCancelSubscriber, CancelJobEvent>(
            cancelJobEvent,
            (subscriber, @event, ct) => subscriber.OnJobCancellationRequestAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishJobCancelledEventAsync(JobCancelledEvent jobCancelledEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobCancelledSubscriber, JobCancelledEvent>(
            jobCancelledEvent,
            (subscriber, @event, ct) => subscriber.OnJobCancelledAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishJobCompletedEventAsync(JobCompletedEvent jobCompletedEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobCompletedSubscriber, JobCompletedEvent>(
            jobCompletedEvent,
            (subscriber, @event, ct) => subscriber.OnJobCompletedAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishJobFaultedEventAsync(JobFaultedEvent jobFaultedEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobFaultedSubscriber, JobFaultedEvent>(
            jobFaultedEvent,
            (subscriber, @event, ct) => subscriber.OnJobFaultedAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishJobProgressEventAsync(JobProgressEvent jobProgressEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobProgressSubscriber, JobProgressEvent>(
            jobProgressEvent,
            (subscriber, @event, ct) => subscriber.OnJobProgressAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishWatchJobEventAsync(JobWatchEvent jobWatchEvent, TimeSpan delay, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay, cancellationToken);
            await ResolveAndInvokeSubscribersAsync<IOnJobWatchSubscriber, JobWatchEvent>(
                jobWatchEvent,
                (subscriber, @event, ct) => subscriber.WatchJobAsync(@event, ct),
                cancellationToken);
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task PublishJobStartedEvent(JobStartedEvent jobStartedEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobStartedSubscriber, JobStartedEvent>(
            jobStartedEvent,
            (subscriber, @event, ct) => subscriber.OnJobStartedAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishJobRestartEvent(JobRestartEvent jobRestartEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobRestartSubscriber, JobRestartEvent>(
            jobRestartEvent,
            (subscriber, @event, ct) => subscriber.OnJobRestartAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    private Task ResolveAndInvokeSubscribersAsync<TSubscriber, TEvent>(
        TEvent @event,
        Func<TSubscriber, TEvent, CancellationToken, Task> func,
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            try
            {
                if (!_scopeFactory.TryCreateScope(out var scope))
                {
                    return;
                }

                using (scope)
                {
                    var subscribers = scope.ServiceProvider.GetServices<TSubscriber>().ToArray();
                    await Task.WhenAll(subscribers.Select(s => func(s, @event, cancellationToken)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error invoking subscribers");
            }
        }, cancellationToken);
    }
}
