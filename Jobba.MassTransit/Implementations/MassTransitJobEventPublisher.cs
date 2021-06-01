using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using MassTransit;

namespace Jobba.MassTransit.Implementations
{
    public class MassTransitJobEventPublisher : IJobEventPublisher
    {
        private readonly IBus _bus;

        public MassTransitJobEventPublisher(IBus bus)
        {
            _bus = bus;
        }

        private Task PublishMessageAsync<T>(T message, CancellationToken cancellationToken) => _bus.Publish(message, cancellationToken);

        public Task PublishJobCancellationRequestAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(cancelJobEvent, cancellationToken);

        public Task PublishJobCancelledEventAsync(JobCancelledEvent jobCancelledEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobCancelledEvent, cancellationToken);

        public Task PublishJobCompletedEventAsync(JobCompletedEvent jobCompletedEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobCompletedEvent, cancellationToken);

        public Task PublishJobFaultedEventAsync(JobFaultedEvent jobFaultedEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobFaultedEvent, cancellationToken);

        public Task PublishJobProgressEventAsync(JobProgressEvent jobProgressEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobProgressEvent, cancellationToken);

        public Task PublishWatchJobEventAsync(JobWatchEvent jobWatchEvent, TimeSpan delay, CancellationToken cancellationToken)
            => PublishMessageAsync(jobWatchEvent, cancellationToken);

        public Task PublishJobStartedEvent(JobStartedEvent jobStartedEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobStartedEvent, cancellationToken);

        public Task PublishJobRestartEvent(JobRestartEvent jobRestartEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobRestartEvent, cancellationToken);
    }
}
