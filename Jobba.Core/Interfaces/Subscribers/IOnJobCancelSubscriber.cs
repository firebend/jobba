using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers;

public interface IOnJobCancelSubscriber<TJob, TJobParams, TJobState>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    public Task<bool> OnJobCancellationRequestAsync(CancelJobEvent<TJob> cancelJobEvent, CancellationToken cancellationToken);
}
