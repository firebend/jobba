using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;

namespace Jobba.MassTransit.Implementations.Consumers
{
    //todo: might need to look into using mass transit request response pattern so we can ask all the consumers in the cluster to cancel the job
    // not just the first consumer to get the message.
    public class OnJobCancelConsumer : AbstractJobbaMassTransitConsumer<CancelJobEvent, IOnJobCancelSubscriber>
    {
        public OnJobCancelConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override Task HandleMessageAsync(IOnJobCancelSubscriber subscriber, CancelJobEvent message, CancellationToken cancellationToken)
            => subscriber.OnJobCancellationRequestAsync(message, cancellationToken);
    }
}
