using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobStartedConsumer : AbstractJobbaMassTransitConsumer<JobStartedEvent, IOnJobStartedSubscriber>
{

    protected override Task HandleMessageAsync(IOnJobStartedSubscriber subscriber, JobStartedEvent message, CancellationToken cancellationToken)
        => subscriber.OnJobStartedAsync(message, cancellationToken);

    public OnJobStartedConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
