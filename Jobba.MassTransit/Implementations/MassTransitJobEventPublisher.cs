using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.MassTransit.Models;
using MassTransit;

namespace Jobba.MassTransit.Implementations
{
    public class MassTransitJobEventPublisher : IJobEventPublisher
    {
        private readonly IBus _bus;
        private readonly IMessageScheduler _messageScheduler;
        private readonly IRequestClient<CancelJobEvent> _requestClient;
        private readonly JobbaMassTransitConfigurationContext _configurationContext;

        public MassTransitJobEventPublisher(IBus bus,
            IMessageScheduler messageScheduler,
            IRequestClient<CancelJobEvent> requestClient,
            JobbaMassTransitConfigurationContext configurationContext)
        {
            _bus = bus;
            _messageScheduler = messageScheduler;
            _requestClient = requestClient;
            _configurationContext = configurationContext;
        }

        public async Task PublishJobCancellationRequestAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken)
        {
            var tries = 0;

            while (tries < _configurationContext.MaxTimesToRequestJobCancellation)
            {
                try
                {
                    var response = await _requestClient.GetResponse<JobbaMassTransitJobCancelRequestResult>(cancelJobEvent, cancellationToken);

                    if (response.Message.WasCancelled)
                    {
                        return;
                    }
                }
                catch
                {
                    await Task.Delay(_configurationContext.CancelJobRequestInterval, cancellationToken);
                    tries++;
                }
            }

        }

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
            => PublishMessageAsync(jobStartedEvent, null, cancellationToken);

        public Task PublishJobRestartEvent(JobRestartEvent jobRestartEvent, CancellationToken cancellationToken)
            => PublishMessageAsync(jobRestartEvent, null, cancellationToken);

        private Task PublishMessageAsync<T>(T message, TimeSpan? delay, CancellationToken cancellationToken)
        {
            if (delay.HasValue)
            {
                var inTheFuture = DateTime.UtcNow.Add(delay.Value);
                return _messageScheduler.SchedulePublish(inTheFuture, message, cancellationToken);
            }

            return _bus.Publish(message, cancellationToken);
        }
    }
}
