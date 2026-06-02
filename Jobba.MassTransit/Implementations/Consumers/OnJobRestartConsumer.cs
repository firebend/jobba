using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobRestartConsumer<TJob, TJobParams, TJobState> : AbstractJobbaMassTransitConsumer<JobRestartEvent<TJob>, IOnJobRestartSubscriber<TJob, TJobParams, TJobState>>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{

    protected override Task HandleMessageAsync(IOnJobRestartSubscriber<TJob, TJobParams, TJobState> subscriber, JobRestartEvent<TJob> message, CancellationToken cancellationToken)
        => subscriber.OnJobRestartAsync(message, cancellationToken);

    public OnJobRestartConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
