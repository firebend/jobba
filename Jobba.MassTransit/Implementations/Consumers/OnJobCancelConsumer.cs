using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces.Subscribers;
using Jobba.MassTransit.Abstractions;
using Jobba.MassTransit.Models;
using MassTransit;

namespace Jobba.MassTransit.Implementations.Consumers
{
    //todo: tests
    public class OnJobCancelConsumer : AbstractJobbaMassTransitConsumer<CancelJobEvent, IOnJobCancelSubscriber>
    {
        private bool _wasCancelled;

        public OnJobCancelConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public override async Task HandleMessageAsync(IOnJobCancelSubscriber subscriber, CancelJobEvent message, CancellationToken cancellationToken)
        {
            if (await subscriber.OnJobCancellationRequestAsync(message, cancellationToken))
            {
                _wasCancelled = true;
            }
        }

        protected override async Task AfterSubscribersAsync(ConsumeContext<CancelJobEvent> context)
        {
            if (_wasCancelled)
            {
                await context
                    .RespondAsync(new JobbaMassTransitJobCancelRequestResult { JobId = context.Message.JobId, WasCancelled = true });
            }
        }
    }
}
