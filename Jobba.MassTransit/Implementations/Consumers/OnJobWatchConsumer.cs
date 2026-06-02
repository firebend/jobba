using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobWatchConsumer<TJob, TJobParams, TJobState> : AbstractJobbaMassTransitConsumer<JobWatchEvent<TJob>, IOnJobWatchSubscriber<TJob, TJobParams, TJobState>>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{

    protected override Task HandleMessageAsync(IOnJobWatchSubscriber<TJob, TJobParams, TJobState> subscriber, JobWatchEvent<TJob> message, CancellationToken cancellationToken)
        => subscriber.WatchJobAsync(message, cancellationToken);

    public OnJobWatchConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
