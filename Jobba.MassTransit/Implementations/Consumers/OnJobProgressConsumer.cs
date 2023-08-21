using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobProgressConsumer : AbstractJobbaMassTransitConsumer<JobProgressEvent, IOnJobProgressSubscriber>
{

    protected override Task HandleMessageAsync(IOnJobProgressSubscriber subscriber, JobProgressEvent message, CancellationToken cancellationToken)
        => subscriber.OnJobProgressAsync(message, cancellationToken);

    public OnJobProgressConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
