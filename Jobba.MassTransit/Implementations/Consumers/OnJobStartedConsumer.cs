using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;

namespace Jobba.MassTransit.Implementations.Consumers
{
    public class OnJobStartedConsumer : AbstractJobbaMassTransitConsumer<JobStartedEvent, IOnJobStartedSubscriber>
    {
        public OnJobStartedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override Task HandleMessageAsync(IOnJobStartedSubscriber subscriber, JobStartedEvent message, CancellationToken cancellationToken)
            => subscriber.OnJobStartedAsync(message, cancellationToken);
    }
}
