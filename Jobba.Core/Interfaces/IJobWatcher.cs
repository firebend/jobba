using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces
{
    public interface IJobWatcher<TJobParams, TJobState>
    {
        Task WatchJobAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
