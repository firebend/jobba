using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobRestartConsumer : AbstractJobbaMassTransitConsumer<JobRestartEvent, IOnJobRestartSubscriber>
{

    protected override Task HandleMessageAsync(IOnJobRestartSubscriber subscriber, JobRestartEvent message, CancellationToken cancellationToken)
        => subscriber.OnJobRestartAsync(message, cancellationToken);

    public OnJobRestartConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
