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

        public DefaultOnJobCancelSubscriber(IJobCancellationTokenStore cancellationTokenStore)
        {
            _cancellationTokenStore = cancellationTokenStore;
        }

        public Task<bool> CancelJobAsync(CancelJobEvent cancelJobEvent, CancellationToken cancellationToken)
        {
            if (cancelJobEvent.JobId == Guid.Empty)
            {
                return Task.FromResult(false);
            }

            var wasCancelled = _cancellationTokenStore.CancelJob(cancelJobEvent.JobId);
            return Task.FromResult(wasCancelled);
        }
    }
}
