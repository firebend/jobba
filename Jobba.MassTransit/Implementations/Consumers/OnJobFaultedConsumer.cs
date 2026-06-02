using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobFaultedConsumer<TJob, TJobParams, TJobState> : AbstractJobbaMassTransitConsumer<JobFaultedEvent<TJob>, IOnJobFaultedSubscriber<TJob>>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{

    protected override Task HandleMessageAsync(IOnJobFaultedSubscriber<TJob> subscriber, JobFaultedEvent<TJob> message, CancellationToken cancellationToken)
        => subscriber.OnJobFaultedAsync(message, cancellationToken);

    public OnJobFaultedConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
