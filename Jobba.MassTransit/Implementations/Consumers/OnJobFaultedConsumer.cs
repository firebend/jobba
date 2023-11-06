using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobFaultedConsumer : AbstractJobbaMassTransitConsumer<JobFaultedEvent, IOnJobFaultedSubscriber>
{

    protected override Task HandleMessageAsync(IOnJobFaultedSubscriber subscriber, JobFaultedEvent message, CancellationToken cancellationToken)
        => subscriber.OnJobFaultedAsync(message, cancellationToken);

    public OnJobFaultedConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
