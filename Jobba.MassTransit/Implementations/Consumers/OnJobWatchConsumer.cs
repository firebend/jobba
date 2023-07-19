using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobWatchConsumer : AbstractJobbaMassTransitConsumer<JobWatchEvent, IOnJobWatchSubscriber>
{
    public OnJobWatchConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    protected override Task HandleMessageAsync(IOnJobWatchSubscriber subscriber, JobWatchEvent message, CancellationToken cancellationToken)
        => subscriber.WatchJobAsync(message, cancellationToken);
}
