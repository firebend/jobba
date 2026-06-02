using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Jobba.MassTransit.Models;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace Jobba.MassTransit.Implementations.Consumers;

public class OnJobCancelConsumer<TJob, TJobParams, TJobState> : AbstractJobbaMassTransitConsumer<CancelJobEvent<TJob>, IOnJobCancelSubscriber<TJob, TJobParams, TJobState>>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    private int _wasCancelled;

    protected override async Task HandleMessageAsync(IOnJobCancelSubscriber<TJob, TJobParams, TJobState> subscriber, CancelJobEvent<TJob> message, CancellationToken cancellationToken)
    {
        if (await subscriber.OnJobCancellationRequestAsync(message, cancellationToken))
        {
            _ = Interlocked.Exchange(ref _wasCancelled, 1);
        }
    }

    protected override async Task AfterSubscribersAsync(ConsumeContext<CancelJobEvent<TJob>> context)
    {
        await context
            .RespondAsync(new JobbaMassTransitJobCancelRequestResult<TJob>
            {
                JobId = context.Message.JobId,
                WasCancelled = Volatile.Read(ref _wasCancelled) != 0
            });
    }

    public OnJobCancelConsumer(IServiceScopeFactory scopeFactory) : base(scopeFactory)
    {
    }
}
