using System.Threading;
using System.Threading.Tasks;
using Jobba.Core.Events;

namespace Jobba.Core.Interfaces.Subscribers;

public interface IOnJobWatchSubscriber<TJob, TJobParams, TJobState>
    where TJob : IJob<TJobParams, TJobState>
    where TJobParams : IJobParams
    where TJobState : IJobState
{
    public Task WatchJobAsync(JobWatchEvent<TJob> jobWatchEvent, CancellationToken cancellationToken);
}
