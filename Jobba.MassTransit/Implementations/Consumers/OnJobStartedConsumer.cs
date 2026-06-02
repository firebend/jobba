using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobStartedConsumer<TJob, TJobParams, TJobState> : AbstractJobbaMassTransitConsumer<JobStartedEvent<TJob>, IOnJobStartedSubscriber<TJob>>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{

    protected override Task HandleMessageAsync(IOnJobStartedSubscriber<TJob> subscriber, JobStartedEvent<TJob> message, CancellationToken cancellationToken)
        => subscriber.OnJobStartedAsync(message, cancellationToken);

    public OnJobStartedConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
