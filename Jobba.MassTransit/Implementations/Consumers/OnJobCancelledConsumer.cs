using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;

namespace Jobba.MassTransit.Implementations.Consumers
{
    public class OnJobCancelledConsumer : AbstractJobbaMassTransitConsumer<JobCancelledEvent, IOnJobCancelledSubscriber>
    {
        public OnJobCancelledConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override Task HandleMessageAsync(IOnJobCancelledSubscriber subscriber, JobCancelledEvent message, CancellationToken cancellationToken)
            => subscriber.OnJobCancelledAsync(message, cancellationToken);
    }
}
