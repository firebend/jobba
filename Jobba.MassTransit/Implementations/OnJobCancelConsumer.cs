using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;

namespace Jobba.MassTransit.Implementations
{
    //todo: test
    public class OnJobCancelConsumer : AbstractJobbaMassTransitConsumer<CancelJobEvent, IOnJobCancelSubscriber>
    {
        public OnJobCancelConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override Task HandleMessageAsync(IOnJobCancelSubscriber subscriber, CancelJobEvent message, CancellationToken cancellationToken)
            => subscriber.OnJobCancellationRequestAsync(message, cancellationToken);
    }
}
