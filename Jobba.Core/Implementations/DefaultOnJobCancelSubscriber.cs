using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;

namespace Jobba.Core.Implementations
{
    public class DefaultOnJobCancelSubscriber : IOnJobCancelSubscriber
    {
        private readonly IJobCancellationTokenStore _cancellationTokenStore;
        private readonly IJobEventPublisher _jobEventPublisher;

        public DefaultOnJobCancelSubscriber(IJobCancellationTokenStore cancellationTokenStore,
            IJobEventPublisher jobEventPublisher)
        {
            _cancellationTokenStore = cancellationTokenStore;
            _jobEventPublisher = jobEventPublisher;
        }

        public Task<bool> CancelJobAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken)
        {
            if (cancelJobEvent.JobId == Guid.Empty)
            {
                return Task.FromResult(false);
            }

            var wasCancelled = _cancellationTokenStore.CancelJob(cancelJobEvent.JobId);

            if (wasCancelled)
            {
                _jobEventPublisher.PublishJobCancelledEventAsync(new JobCancelledEvent(cancelJobEvent.JobId), cancellationToken);
            }

            return Task.FromResult(wasCancelled);
        }
    }
}
