using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;

namespace Jobba.MassTransit.Implementations.Consumers
{
    public class OnJobFaultedConsumer : AbstractJobbaMassTransitConsumer<JobFaultedEvent, IOnJobFaultedSubscriber>
    {
        public OnJobFaultedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override Task HandleMessageAsync(IOnJobFaultedSubscriber subscriber, JobFaultedEvent message, CancellationToken cancellationToken)
            => subscriber.OnJobFaultedAsync(message, cancellationToken);
    }
}
