using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobCancelledConsumer<TJob, TJobParams, TJobState> : AbstractJobbaMassTransitConsumer<JobCancelledEvent<TJob>, IOnJobCancelledSubscriber<TJob>>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    protected override Task HandleMessageAsync(IOnJobCancelledSubscriber<TJob> subscriber, JobCancelledEvent<TJob> message, CancellationToken cancellationToken)
        => subscriber.OnJobCancelledAsync(message, cancellationToken);

    public OnJobCancelledConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
