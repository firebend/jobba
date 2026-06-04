using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobCompleteConsumer<TJob, TJobParams, TJobState> : AbstractJobbaMassTransitConsumer<JobCompletedEvent<TJob>, IOnJobCompletedSubscriber<TJob>>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{

    protected override Task HandleMessageAsync(IOnJobCompletedSubscriber<TJob> subscriber, JobCompletedEvent<TJob> message, CancellationToken cancellationToken)
        => subscriber.OnJobCompletedAsync(message, cancellationToken);

    public OnJobCompleteConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
