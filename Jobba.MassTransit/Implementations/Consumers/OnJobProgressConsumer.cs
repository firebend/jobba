using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobProgressConsumer<TJob, TJobParams, TJobState> : AbstractJobbaMassTransitConsumer<JobProgressEvent<TJob>, IOnJobProgressSubscriber<TJob>>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{

    protected override Task HandleMessageAsync(IOnJobProgressSubscriber<TJob> subscriber, JobProgressEvent<TJob> message, CancellationToken cancellationToken)
        => subscriber.OnJobProgressAsync(message, cancellationToken);

    public OnJobProgressConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
