using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobCancelledConsumer : AbstractJobbaMassTransitConsumer<JobCancelledEvent, IOnJobCancelledSubscriber>
{
    protected override Task HandleMessageAsync(IOnJobCancelledSubscriber subscriber, JobCancelledEvent message, CancellationToken cancellationToken)
        => subscriber.OnJobCancelledAsync(message, cancellationToken);

    public OnJobCancelledConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
