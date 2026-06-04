using System;
using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;
using Jobba.Core.Interfaces;
using Jobba.Core.Interfaces.Subscribers;

namespace Jobba.Core.Implementations;

public class DefaultOnJobCancelSubscriber<TJob, TJobParams, TJobState> : AbstractJobbaEventSubscriber, IOnJobCancelSubscriber<TJob, TJobParams, TJobState>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    private readonly IJobCancellationTokenStore _cancellationTokenStore;
    private readonly IJobEventPublisher _jobEventPublisher;

    public DefaultOnJobCancelSubscriber(IJobCancellationTokenStore cancellationTokenStore,
        IJobEventPublisher jobEventPublisher,
        IJobSystemInfoProvider systemInfoProvider)
        : base(systemInfoProvider)
    {
        _cancellationTokenStore = cancellationTokenStore;
        _jobEventPublisher = jobEventPublisher;
    }

    public async Task<bool> OnJobCancellationRequestAsync(CancelJobEvent<TJob> cancelJobEvent, CancellationToken cancellationToken)
    {
        if (!ShouldProcessEvent(cancelJobEvent))
        {
            return false;
        }

        if (cancelJobEvent.JobId == Guid.Empty)
        {
            return false;
        }

        var wasCancelled = _cancellationTokenStore.CancelJob(cancelJobEvent.JobId);

        if (wasCancelled)
        {
            await _jobEventPublisher.PublishJobCancelledEventAsync(
                new JobCancelledEvent<TJob>(cancelJobEvent.JobId, cancelJobEvent.JobRegistrationId)
                {
                    SystemMoniker = cancelJobEvent.SystemMoniker
                },
                cancellationToken);
        }

        return wasCancelled;
    }
}
