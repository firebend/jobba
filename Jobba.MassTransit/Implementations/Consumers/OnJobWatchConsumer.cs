using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobWatchConsumer : AbstractJobbaMassTransitConsumer<JobWatchEvent, IOnJobWatchSubscriber>
{

    protected override Task HandleMessageAsync(IOnJobWatchSubscriber subscriber, JobWatchEvent message, CancellationToken cancellationToken)
        => subscriber.WatchJobAsync(message, cancellationToken);

    public OnJobWatchConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
