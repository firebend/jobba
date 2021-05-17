using System;
using System.Threading;
using System.Threading.Tasks;

namespace Jobba.Core.Interfaces.Subscribers
{
    public interface IOnJobWatchSubscriber
    {
        Task WatchJobAsync(Guid jobId, CancellationToken cancellationToken);
    }
}
