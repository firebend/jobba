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


    public Task PublishJobCancellationRequestAsync<TJob, TJobParams, TJobState>(CancelJobEvent<TJob> cancelJobEvent, CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobCancelSubscriber<TJob, TJobParams, TJobState>, CancelJobEvent<TJob>>(
            cancelJobEvent,
            (subscriber, @event, ct) => subscriber.OnJobCancellationRequestAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishJobCancelledEventAsync<TJob>(JobCancelledEvent<TJob> jobCancelledEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobCancelledSubscriber<TJob>, JobCancelledEvent<TJob>>(
            jobCancelledEvent,
            (subscriber, @event, ct) => subscriber.OnJobCancelledAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishJobCompletedEventAsync<TJob>(JobCompletedEvent<TJob> jobCompletedEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobCompletedSubscriber<TJob>, JobCompletedEvent<TJob>>(
            jobCompletedEvent,
            (subscriber, @event, ct) => subscriber.OnJobCompletedAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishJobFaultedEventAsync<TJob>(JobFaultedEvent<TJob> jobFaultedEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobFaultedSubscriber<TJob>, JobFaultedEvent<TJob>>(
            jobFaultedEvent,
            (subscriber, @event, ct) => subscriber.OnJobFaultedAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishJobProgressEventAsync<TJob>(JobProgressEvent<TJob> jobProgressEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobProgressSubscriber<TJob>, JobProgressEvent<TJob>>(
            jobProgressEvent,
            (subscriber, @event, ct) => subscriber.OnJobProgressAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishWatchJobEventAsync<TJob, TJobParams, TJobState>(JobWatchEvent<TJob> jobWatchEvent, TimeSpan delay, CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(delay, cancellationToken);
            await ResolveAndInvokeSubscribersAsync<IOnJobWatchSubscriber<TJob, TJobParams, TJobState>, JobWatchEvent<TJob>>(
                jobWatchEvent,
                (subscriber, @event, ct) => subscriber.WatchJobAsync(@event, ct),
                cancellationToken);
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task PublishJobStartedEvent<TJob>(JobStartedEvent<TJob> jobStartedEvent, CancellationToken cancellationToken)
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobStartedSubscriber<TJob>, JobStartedEvent<TJob>>(
            jobStartedEvent,
            (subscriber, @event, ct) => subscriber.OnJobStartedAsync(@event, ct),
            cancellationToken);
        return Task.CompletedTask;
    }

    public Task PublishJobRestartEvent<TJob, TJobParams, TJobState>(JobRestartEvent<TJob> jobRestartEvent, CancellationToken cancellationToken)
        where TJob : IJob<TJobParams, TJobState>
        where TJobParams : IJobParams
        where TJobState : IJobState
    {
        _ = ResolveAndInvokeSubscribersAsync<IOnJobRestartSubscriber<TJob, TJobParams, TJobState>, JobRestartEvent<TJob>>(
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
