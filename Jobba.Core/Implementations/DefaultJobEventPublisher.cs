using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jobba.Core.Implementations;

public class DefaultJobEventPublisher : IJobEventPublisher, IDisposable
{
    private readonly ILogger<DefaultJobEventPublisher> _logger;
    private readonly IServiceScope _scope;

    public DefaultJobEventPublisher(IServiceProvider serviceProvider,
        ILogger<DefaultJobEventPublisher> logger)
    {
        _scope = serviceProvider.CreateScope();
        _logger = logger;
    }

    public void Dispose()
    {
        _scope?.Dispose();
        GC.SuppressFinalize(this);
    }


    public Task PublishJobCancellationRequestAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken)
    {
        ResolveAndInvokeSubscribers<IOnJobCancelSubscriber, CancelJobEvent>(
            cancelJobEvent,
            (subscriber, @event, ct) => subscriber.OnJobCancellationRequestAsync(@event, ct),
            cancellationToken);

        return Task.CompletedTask;
    }

    public Task PublishJobCancelledEventAsync(JobCancelledEvent jobCancelledEvent, CancellationToken cancellationToken)
    {
        ResolveAndInvokeSubscribers<IOnJobCancelledSubscriber, JobCancelledEvent>(
            jobCancelledEvent,
            (subscriber, @event, ct) => subscriber.OnJobCancelledAsync(@event, ct),
            cancellationToken);

        return Task.CompletedTask;
    }

    public Task PublishJobCompletedEventAsync(JobCompletedEvent jobCompletedEvent, CancellationToken cancellationToken)
    {
        ResolveAndInvokeSubscribers<IOnJobCompletedSubscriber, JobCompletedEvent>(
            jobCompletedEvent,
            (subscriber, @event, ct) => subscriber.OnJobCompletedAsync(@event, ct),
            cancellationToken);

        return Task.CompletedTask;
    }

    public Task PublishJobFaultedEventAsync(JobFaultedEvent jobFaultedEvent, CancellationToken cancellationToken)
    {
        ResolveAndInvokeSubscribers<IOnJobFaultedSubscriber, JobFaultedEvent>(
            jobFaultedEvent,
            (subscriber, @event, ct) => subscriber.OnJobFaultedAsync(@event, ct),
            cancellationToken);

        return Task.CompletedTask;
    }

    public Task PublishJobProgressEventAsync(JobProgressEvent jobProgressEvent, CancellationToken cancellationToken)
    {
        ResolveAndInvokeSubscribers<IOnJobProgressSubscriber, JobProgressEvent>(
            jobProgressEvent,
            (subscriber, @event, ct) => subscriber.OnJobProgressAsync(@event, ct),
            cancellationToken);

        return Task.CompletedTask;
    }

    public Task PublishWatchJobEventAsync(JobWatchEvent jobWatchEvent, TimeSpan delay, CancellationToken cancellationToken)
    {
        var _ = Task.Run(async () =>
        {
            await Task.Delay(delay, cancellationToken);

            ResolveAndInvokeSubscribers<IOnJobWatchSubscriber, JobWatchEvent>(
                jobWatchEvent,
                (subscriber, @event, ct) => subscriber.WatchJobAsync(@event, ct),
                cancellationToken);
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task PublishJobStartedEvent(JobStartedEvent jobStartedEvent, CancellationToken cancellationToken)
    {
        ResolveAndInvokeSubscribers<IOnJobStartedSubscriber, JobStartedEvent>(
            jobStartedEvent,
            (subscriber, @event, ct) => subscriber.OnJobStartedAsync(@event, ct),
            cancellationToken);

        return Task.CompletedTask;
    }

    public Task PublishJobRestartEvent(JobRestartEvent jobRestartEvent, CancellationToken cancellationToken)
    {
        ResolveAndInvokeSubscribers<IOnJobRestartSubscriber, JobRestartEvent>(
            jobRestartEvent,
            (subscriber, @event, ct) => subscriber.OnJobRestartAsync(@event, ct),
            cancellationToken);

        return Task.CompletedTask;
    }

    private TSubscriber[] GetSubscribers<TSubscriber>()
    {
        try
        {
            var subscribers = _scope.ServiceProvider.GetServices<TSubscriber>();
            var subscribersArray = subscribers.ToArray();
            return subscribersArray;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error getting subscribers");
            throw;
        }
    }

    private static void InvokeSubscribers<TSubscriber, TEvent>(IEnumerable<TSubscriber> subscribers,
        TEvent @event,
        Func<TSubscriber, TEvent, CancellationToken, Task> func,
        CancellationToken cancellationToken)
    {
        var _ = Task.Run(async () =>
        {
            var tasks = subscribers.Select(x => func(x, @event, cancellationToken));
            await Task.WhenAll(tasks);
        }, cancellationToken);
    }


    private void ResolveAndInvokeSubscribers<TSubscriber, TEvent>(
        TEvent @event,
        Func<TSubscriber, TEvent, CancellationToken, Task> func,
        CancellationToken cancellationToken)
    {
        var subscribers = GetSubscribers<TSubscriber>();
        InvokeSubscribers(subscribers, @event, func, cancellationToken);
    }
}
