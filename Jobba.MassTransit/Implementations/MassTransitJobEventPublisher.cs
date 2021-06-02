using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using MassTransit;

namespace Jobba.MassTransit.Implementations
{
    //todo: test
    public class MassTransitJobEventPublisher : IJobEventPublisher
    {
        private readonly IBus _bus;
        private readonly IMessageScheduler _messageScheduler;

        public MassTransitJobEventPublisher(IBus bus,
            IMessageScheduler messageScheduler)
        {
            _bus = bus;
            _messageScheduler = messageScheduler;
        }

        private Task PublishMessageAsync<T>(T message, TimeSpan? delay,  CancellationToken cancellationToken)
            => delay.HasValue
                ? _messageScheduler.SchedulePublish(DateTime.UtcNow.Add(delay.Value), message, cancellationToken)
                : _bus.Publish(message, cancellationToken);

        public Task PublishJobCancellationRequestAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(cancelJobEvent, null, cancellationToken);

        public Task PublishJobCancelledEventAsync(JobCancelledEvent jobCancelledEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobCancelledEvent, null, cancellationToken);

        public Task PublishJobCompletedEventAsync(JobCompletedEvent jobCompletedEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobCompletedEvent, null, cancellationToken);

        public Task PublishJobFaultedEventAsync(JobFaultedEvent jobFaultedEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobFaultedEvent, null, cancellationToken);

        public Task PublishJobProgressEventAsync(JobProgressEvent jobProgressEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobProgressEvent, null, cancellationToken);

        public Task PublishWatchJobEventAsync(JobWatchEvent jobWatchEvent, TimeSpan delay, CancellationToken cancellationToken)
            => PublishMessageAsync(jobWatchEvent, delay, cancellationToken);

        public Task PublishJobStartedEvent(JobStartedEvent jobStartedEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobStartedEvent, null,  cancellationToken);

        public Task PublishJobRestartEvent(JobRestartEvent jobRestartEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobRestartEvent, null, cancellationToken);
    }
}
