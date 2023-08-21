using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobCompleteConsumer : AbstractJobbaMassTransitConsumer<JobCompletedEvent, IOnJobCompletedSubscriber>
{

    protected override Task HandleMessageAsync(IOnJobCompletedSubscriber subscriber, JobCompletedEvent message, CancellationToken cancellationToken)
        => subscriber.OnJobCompletedAsync(message, cancellationToken);

    public OnJobCompleteConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
