using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;

namespace Jobba.MassTransit.Implementations.Consumers
{
    public class OnJobRestartedConsumer : AbstractJobbaMassTransitConsumer<JobRestartEvent, IOnJobRestartSubscriber>
    {
        public OnJobRestartedConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override Task HandleMessageAsync(IOnJobRestartSubscriber subscriber, JobRestartEvent message, CancellationToken cancellationToken)
            => subscriber.OnJobRestartAsync(message, cancellationToken);
    }
}
