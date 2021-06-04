using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;

namespace Jobba.MassTransit.Implementations
{
    public class OnJobProgressConsumer : AbstractJobbaMassTransitConsumer<JobProgressEvent, IOnJobProgressSubscriber>
    {
        public OnJobProgressConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override Task HandleMessageAsync(IOnJobProgressSubscriber subscriber, JobProgressEvent message, CancellationToken cancellationToken)
            => subscriber.OnJobProgressAsync(message, cancellationToken);
    }
}
