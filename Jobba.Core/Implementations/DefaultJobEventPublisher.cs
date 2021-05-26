using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.Core.Implementations
{
    public class DefaultJobEventPublisher : IJobEventPublisher
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultJobEventPublisher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private TSubscriber[] GetSubscribers<TSubscriber>()
        {
            using var scope = _serviceProvider.CreateScope();
            var subscribers = scope.ServiceProvider.GetServices<TSubscriber>();
            var subscribersArray = subscribers.ToArray();

            return subscribersArray;
        }

        private static async Task InvokeSubscribersAsync<TSubscriber, TEvent>(IEnumerable<TSubscriber> subscribers,
            TEvent @event,
            Func<TSubscriber, TEvent, CancellationToken, Task> func,
            CancellationToken cancellationToken)
        {
            var tasks = subscribers.Select(x => func(x, @event, cancellationToken));
            await Task.WhenAll(tasks);
        }


        private async Task ResolveAndInvokeSubscribersAsync<TSubscriber, TEvent>(
            TEvent @event,
            Func<TSubscriber, TEvent, CancellationToken, Task> func,
            CancellationToken cancellationToken)
        {
            var subscribers = GetSubscribers<TSubscriber>();
            await InvokeSubscribersAsync(subscribers, @event, func, cancellationToken);
        }


        public Task PublishJobCancellationRequestAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken) =>
            ResolveAndInvokeSubscribersAsync<IOnJobCancelSubscriber, CancelJobEvent>(
                cancelJobEvent,
                (subscriber, @event, ct) => subscriber.OnJobCancellationRequestAsync(@event, ct),
                cancellationToken);

        public Task PublishJobCancelledEventAsync(JobCancelledEvent jobCancelledEvent, CancellationToken cancellationToken) =>
            ResolveAndInvokeSubscribersAsync<IOnJobCancelledSubscriber, JobCancelledEvent>(
                jobCancelledEvent,
                (subscriber, @event, ct) => subscriber.OnJobCancelledAsync(@event, ct),
                cancellationToken);

        public Task PublishJobCompletedEventAsync(JobCompletedEvent jobCompletedEvent, CancellationToken cancellationToken) =>
            ResolveAndInvokeSubscribersAsync<IOnJobCompletedSubscriber, JobCompletedEvent>(
                jobCompletedEvent,
                (subscriber, @event, ct) => subscriber.OnJobCompletedAsync(@event, ct),
                cancellationToken);

        public Task PublishJobFaultedEventAsync(JobFaultedEvent jobFaultedEvent, CancellationToken cancellationToken) =>
            ResolveAndInvokeSubscribersAsync<IOnJobFaultedSubscriber, JobFaultedEvent>(
                jobFaultedEvent,
                (subscriber, @event, ct) => subscriber.OnJobFaultedAsync(@event, ct),
                cancellationToken);

        public Task PublishJobProgressEventAsync(JobProgressEvent jobProgressEvent, CancellationToken cancellationToken) =>
            ResolveAndInvokeSubscribersAsync<IOnJobProgressSubscriber, JobProgressEvent>(
                jobProgressEvent,
                (subscriber, @event, ct) => subscriber.OnJobProgressAsync(@event, ct),
                cancellationToken);

        public Task PublishWatchJobEventAsync(JobWatchEvent jobWatchEvent, TimeSpan delay, CancellationToken cancellationToken) =>
            ResolveAndInvokeSubscribersAsync<IOnJobWatchSubscriber, JobWatchEvent>(
                jobWatchEvent,
                (subscriber, @event, ct) => subscriber.WatchJobAsync(@event, ct),
                cancellationToken);

        public Task PublishJobStartedEvent(JobStartedEvent jobStartedEvent, CancellationToken cancellationToken) =>
            ResolveAndInvokeSubscribersAsync<IOnJobStartedSubscriber, JobStartedEvent>(
                jobStartedEvent,
                (subscriber, @event, ct) => subscriber.OnJobStartedAsync(@event, ct),
                cancellationToken);

        public Task PublishJobRestartEvent(JobRestartEvent jobRestartEvent, CancellationToken cancellationToken) =>
            ResolveAndInvokeSubscribersAsync<IOnJobRestartSubscriber, JobRestartEvent>(
                jobRestartEvent,
                (subscriber, @event, ct) => subscriber.OnJobRestartAsync(@event, ct),
                cancellationToken);
    }
}
